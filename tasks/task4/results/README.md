# Развёртывание booking-service в Minikube

## 📋 Требования

```bash
brew install docker minikube helm kubectl
```

## 🚀 Быстрый старт

### 1. Запустить Minikube
```bash
minikube start --driver=docker
minikube status
```

### 2. Собрать и загрузить образ
```bash
cd tasks/task4/booking-service
docker build -t booking-service:latest .
minikube image load booking-service:latest
```

### 3. Развернуть Helm-чарт (staging)
```bash
cd ../../..
helm upgrade --install booking-service ./tasks/task4/helm/booking-service \
  -f ./tasks/task4/helm/booking-service/values.stage.yaml
```

### 4. Проверить развёртывание
```bash
bash tasks/task4/check-status.sh
```

### 5. Протестировать сервис
```bash
# Терминал 1
kubectl port-forward svc/booking-service 8080:80

# Терминал 2
curl http://localhost:8080/ping  # pong
```

## 🔄 CI/CD пайплайн

```bash
npm install -g gitlab-ci-local
gitlab-ci-local
```

Стадии: `build` → `test` → `deploy` → `tag`

## 📦 Конфигурации

**values.stage.yaml** — 1 реплика, feature flag отключен  
**values.prod.yaml** — продакшн конфиг, feature flag включен

## 🌐 Service Discovery (DNS)

```bash
bash tasks/task4/check-dns.sh
```

## 🛠️ Команды управления

```bash
# Масштабирование

# Обновление
helm upgrade booking-service ./tasks/task4/helm/booking-service \
  -f ./tasks/task4/helm/booking-service/values.stage.yaml

# История
helm history booking-service

# Откат
helm rollback booking-service 1

# Удаление
helm uninstall booking-service
```

## 📊 Мониторинг

```bash
kubectl get pods -l app=booking-service
kubectl describe pod -l app=booking-service
kubectl logs -l app=booking-service -f
helm list
```

## 🧹 Очистка

```bash
helm uninstall booking-service
minikube stop
minikube delete
```

---

**Версия:** 1.0 | **Дата:** 15 февраля 2026
