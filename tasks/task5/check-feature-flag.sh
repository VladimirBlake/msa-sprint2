#!/bin/bash

set -e

RETRIES=10
SVC="http://booking-service"

pass() { echo "✅ $1"; }
fail() { echo "❌ $1"; exit 1; }

echo "▶️ Проверка Feature Flag маршрутизации через EnvoyFilter"
echo "============================================="
echo ""
echo "ℹ️  Все запросы выполняются изнутри кластера через Istio sidecar,"
echo "   чтобы VirtualService и EnvoyFilter применялись корректно."

# --- Тест 1: запрос БЕЗ заголовка → должен попасть на v1 ---
echo ""
echo "🔹 Тест 1: Запрос без X-Feature-Enabled (ожидается v1)"

RESULT=$(kubectl run ff-test-no-flag --rm -i --restart=Never --image=curlimages/curl -- \
  sh -c "
    CODE=\$(curl -s -o /dev/null -w '%{http_code}' $SVC/ping)
    FEAT=\$(curl -s -o /dev/null -w '%{http_code}' $SVC/feature)
    echo \"PING=\$CODE FEATURE=\$FEAT\"
  " 2>/dev/null | grep "PING=")

PING_CODE=$(echo "$RESULT" | sed -n 's/.*PING=\([0-9]*\).*/\1/p')
FEAT_CODE=$(echo "$RESULT" | sed -n 's/.*FEATURE=\([0-9]*\).*/\1/p')

if [ "$PING_CODE" = "200" ]; then
  pass "GET /ping → 200 OK"
else
  fail "GET /ping → $PING_CODE (ожидалось 200)"
fi

if [ "$FEAT_CODE" != "200" ]; then
  pass "GET /feature без флага → $FEAT_CODE (v1 не имеет /feature)"
else
  fail "GET /feature без флага вернул 200 — трафик попал на v2 вместо v1"
fi

# --- Тест 2: запрос С заголовком → должен попасть на v2 ---
echo ""
echo "🔹 Тест 2: Запрос с X-Feature-Enabled: true (ожидается v2)"

RESULT=$(kubectl run ff-test-with-flag --rm -i --restart=Never --image=curlimages/curl -- \
  sh -c "
    CODE=\$(curl -s -o /dev/null -w '%{http_code}' -H 'X-Feature-Enabled: true' $SVC/ping)
    BODY=\$(curl -s -H 'X-Feature-Enabled: true' $SVC/feature)
    echo \"PING=\$CODE BODY=\$BODY\"
  " 2>/dev/null | grep "PING=")

PING_CODE=$(echo "$RESULT" | sed -n 's/.*PING=\([0-9]*\).*/\1/p')
BODY=$(echo "$RESULT" | sed -n 's/.*BODY=\(.*\)/\1/p')

if [ "$PING_CODE" = "200" ]; then
  pass "GET /ping с флагом → 200 OK (v2)"
else
  fail "GET /ping с флагом → $PING_CODE (ожидалось 200)"
fi

if echo "$BODY" | grep -q "Feature X is enabled"; then
  pass "GET /feature с флагом → '$BODY' (v2 — фича активна)"
else
  fail "GET /feature с флагом → '$BODY' (ожидалось 'Feature X is enabled!')"
fi

# --- Тест 3: множественная проверка распределения трафика ---
echo ""
echo "🔹 Тест 3: Проверка маршрутизации ($RETRIES запросов)"

kubectl run ff-test-distribution --rm -it --restart=Never --image=curlimages/curl -- \
  sh -c "
echo '--- С заголовком X-Feature-Enabled: true → /feature ---';
for i in \$(seq 1 $RETRIES); do
  curl -s -H 'X-Feature-Enabled: true' $SVC/feature;
  echo;
done | sort | uniq -c;
echo '';
echo '--- Без заголовка → /ping ---';
for i in \$(seq 1 $RETRIES); do
  curl -s $SVC/ping;
  echo;
done | sort | uniq -c
"

echo ""
echo "============================================="
echo "✅ Проверка Feature Flag завершена"
