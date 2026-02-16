# Task 5 — Istio Service Mesh для booking-service

## Что было сделано

### 1. Установка Istio

```bash
istioctl install --set profile=demo
kubectl label namespace default istio-injection=enabled
```

Автоматическая инъекция sidecar (Envoy) включена в `default` namespace.

### 2. Две версии сервиса

На основе task4 развёрнуты два Deployment через Helm:

- **v1** (`values-v1.yaml`) — основная версия, `ENABLE_FEATURE_X=false`
- **v2** (`values-v2.yaml`) — новая версия, `ENABLE_FEATURE_X=true`, эндпоинт `/feature` активен

### 3. Канареечный релиз (VirtualService)

Файл: `virtual-service.yaml`

- 90% трафика → v1, 10% → v2
- Retries: до 5 попыток при `5xx`, `connect-failure`, `gateway-error`
- Timeout: 15s

### 4. Fallback и Circuit Breaking (DestinationRule)

Файл: `destination-rule.yaml`

- **Outlier Detection** на уровне subset v1 и глобально: при 1 ошибке 5xx/gateway endpoint исключается на 60s
- `maxEjectionPercent: 100` — при сбое все endpoint'ы v1 могут быть исключены, трафик уходит на v2
- **Connection Pool** для v1: `maxRequestsPerConnection: 1`

### 5. Фича-флаги через EnvoyFilter

Файл: `envoy-filter.yaml`

- Lua-фильтр на sidecar: при наличии заголовка `X-Feature-Enabled: true` трафик направляется на v2
- Маршрутизация на уровне Envoy VIRTUAL_HOST: match по заголовку → cluster v2

### 6. Проверка

```bash
./check-istio.sh          # Istio установлен, инъекция включена
./check-canary.sh         # 90/10 распределение трафика
./check-fallback.sh       # Fallback: при сбое v1 трафик идёт на v2
./check-feature-flag.sh   # X-Feature-Enabled: true → v2
```

## Созданные ресурсы

| Файл | Тип | Назначение |
|------|-----|------------|
| `virtual-service.yaml` | VirtualService | Канареечный 90/10 + feature flag routing + retries |
| `destination-rule.yaml` | DestinationRule | Subsets v1/v2, outlier detection, circuit breaking |
| `envoy-filter.yaml` | EnvoyFilter | Lua-фильтр для маршрутизации по заголовку X-Feature-Enabled |

## Применение

```bash
kubectl apply -f results/virtual-service.yaml
kubectl apply -f results/destination-rule.yaml
kubectl apply -f results/envoy-filter.yaml
```
