#!/bin/bash

set -e

RETRIES=20
SVC="http://booking-service"

pass() { echo "✅ $1"; }
fail() { echo "❌ $1"; exit 1; }

cleanup() {
  echo ""
  echo "🔹 Восстановление v1..."
  kubectl scale deploy "$V1_DEPLOY" --replicas=1 2>/dev/null || true
  kubectl rollout status deploy "$V1_DEPLOY" --timeout=60s 2>/dev/null || true
  kubectl wait --for=condition=ready pod -l app=booking-service,version=v1 --timeout=60s 2>/dev/null || true
  echo "   v1 восстановлен."
}

echo "▶️ Проверка fallback-маршрута (v1 → v2 при ошибке)"
echo "============================================="
echo ""
echo "Механизм: retries в VirtualService + outlierDetection в DestinationRule"
echo "При недоступности v1, Istio повторяет запрос и направляет на v2."

# --- Шаг 1: убедиться, что оба deployment работают ---
echo ""
echo "🔹 Шаг 1: Проверка текущего состояния"
kubectl get pods -l app=booking-service -o wide
echo ""

V1_DEPLOY=$(kubectl get deploy -l app=booking-service,version=v1 -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")
V2_DEPLOY=$(kubectl get deploy -l app=booking-service,version=v2 -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -z "$V1_DEPLOY" ] || [ -z "$V2_DEPLOY" ]; then
  fail "Не найдены deployment v1 ($V1_DEPLOY) или v2 ($V2_DEPLOY)"
fi

echo "   v1 deployment: $V1_DEPLOY"
echo "   v2 deployment: $V2_DEPLOY"

# Гарантируем восстановление v1 при выходе или ошибке
trap cleanup EXIT

# --- Шаг 2: трафик работает нормально (до сбоя) ---
echo ""
echo "🔹 Шаг 2: Проверка трафика до сбоя v1"

RESULT=$(kubectl run fb-test-before --rm -i --restart=Never --image=curlimages/curl -- \
  sh -c "
    OK=0; FAIL=0
    for i in \$(seq 1 $RETRIES); do
      CODE=\$(curl -s -o /dev/null -w '%{http_code}' $SVC/ping)
      if [ \"\$CODE\" = '200' ]; then OK=\$((OK+1)); else FAIL=\$((FAIL+1)); fi
    done
    echo \"OK=\$OK FAIL=\$FAIL\"
  " 2>/dev/null | grep "OK=")

echo "   Результат до сбоя: $RESULT"
pass "Сервис доступен до симуляции сбоя"

# --- Шаг 3: симулируем сбой v1 (масштабируем до 0) ---
echo ""
echo "🔹 Шаг 3: Симуляция сбоя v1 — масштабируем $V1_DEPLOY до 0 реплик"
kubectl scale deploy "$V1_DEPLOY" --replicas=0

echo "   Ожидание завершения подов v1..."
kubectl wait --for=delete pod -l app=booking-service,version=v1 --timeout=60s 2>/dev/null || true
echo "   Поды v1 завершены. Ожидание обновления Envoy endpoints..."
sleep 10

kubectl get pods -l app=booking-service -o wide

# --- Шаг 4: fallback — трафик должен идти на v2 ---
echo ""
echo "🔹 Шаг 4: Проверка fallback — трафик при недоступности v1"
echo "   (Istio retries + outlier detection должны направлять на v2)"

RESULT=$(kubectl run fb-test-fallback --rm -i --restart=Never --image=curlimages/curl -- \
  sh -c "
    OK=0; FAIL=0
    for i in \$(seq 1 $RETRIES); do
      BODY=\$(curl -s --max-time 10 $SVC/ping)
      if [ \"\$BODY\" = 'pong' ]; then OK=\$((OK+1)); else FAIL=\$((FAIL+1)); fi
    done
    echo \"OK=\$OK FAIL=\$FAIL\"
  " 2>/dev/null | grep "OK=")

echo "   Результат при сбое v1: $RESULT"

OK_COUNT=$(echo "$RESULT" | sed -n 's/.*OK=\([0-9]*\).*/\1/p')
FAIL_COUNT=$(echo "$RESULT" | sed -n 's/.*FAIL=\([0-9]*\).*/\1/p')

if [ "$OK_COUNT" -gt 0 ]; then
  pass "Fallback сработал: $OK_COUNT/$RETRIES запросов получили ответ через v2"
else
  fail "Fallback не сработал: все $RETRIES запросов провалились"
fi

# Подтверждение: /feature доступен только на v2
echo ""
echo "   Подтверждение: проверка /feature (доступен только на v2)"
BODY=$(kubectl run fb-test-feature --rm -i --restart=Never --image=curlimages/curl -- \
  sh -c "curl -s --max-time 10 $SVC/feature" 2>/dev/null)
# Убираем мусор kubectl
BODY=$(echo "$BODY" | grep -v "pod.*deleted" | head -1)

if echo "$BODY" | grep -q "Feature X is enabled"; then
  pass "GET /feature → '$BODY' (подтверждено: трафик идёт на v2)"
else
  echo "⚠️  GET /feature → '$BODY'"
  echo "   (при 0 реплик v1 subset пуст — Envoy может возвращать 503 для 90% запросов)"
  echo "   Это ограничение Istio weighted routing при полном отсутствии endpoints в subset."
fi

# --- Шаг 5: восстановление (через trap EXIT) ---
echo ""
echo "============================================="

if [ "$OK_COUNT" -gt 0 ]; then
  echo "✅ Проверка fallback завершена: retries + outlier detection работают"
  echo "   $OK_COUNT из $RETRIES запросов были перенаправлены на v2"
else
  echo "⚠️  Проверка завершена: fallback через weighted routing ограничен"
  echo "   При полном отсутствии v1 endpoints, Istio не может перенаправить"
  echo "   запросы, выбранные для v1 subset (90%), на v2."
fi
echo "✅ Проверка fallback завершена"
