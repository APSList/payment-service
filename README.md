# payment-service

> Mikrostoritev za upravljanje plačil in integracijo s Stripe.

---

## Odgovornosti

`payment-service` izpostavlja HTTP API za kreiranje in potrjevanje plačil ter se integrira s **Stripe** za obdelavo transakcij.  
Podatke o plačilih hrani v **PostgreSQL** (Supabase) in po potrebi objavlja dogodke v **Kafka** prek **Outbox** vzorca.

Glavne odgovornosti zajemajo:
- Kreiranje in upravljanje plačil (payment intents)
- Obdelava Stripe webhook-ov
- Ročno posodabljanje statusa plačil
- Kreiranje in pregled potrdil o plačilih

---

## Tehnološki sklad

- **.NET / ASP.NET Core**
- **Entity Framework Core** + **Npgsql**
- **PostgreSQL** (Supabase)
- **Stripe**
- **Kafka** (opcijsko)
- **Serilog** (JSON logi)
- **HealthChecks** (liveness/readiness)
- **Prometheus** (prometheus-net)

---

## API

### Swagger
- Swagger UI: `/swagger`
- OpenAPI JSON: `/swagger/v1/swagger.json`

### Model napak
- Kjer je smiselno, servis uporablja standardni ASP.NET Core `ProblemDetails`.

---

## Konfiguracija

Servis uporablja **Options pattern** (`IOptions<T>`) in ob zagonu validira nastavitve (`ValidateOnStart`, data annotations, dodatne validacije), zato ob napačni konfiguraciji **ne zažene** aplikacije.

### Nastavitve (appsettings)

> Pri env var se `:` zamenja z `__` (npr. `Stripe__ApiKey`).

#### ConnectionStrings
- `ConnectionStrings:Supabase` — PostgreSQL/Supabase connection string.

#### Logging
- `Logging:LogLevel:Default` — privzeti nivo logiranja.
- `Logging:LogLevel:Microsoft.AspNetCore` — nivo logiranja za ASP.NET Core.
- `Logging:LogLevel:Microsoft.EntityFrameworkCore` — nivo logiranja za EF Core.
- `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` — nivo logiranja SQL.

#### Stripe
- `Stripe:ApiKey` — Stripe secret key.
- `Stripe:WebhookSecret` — secret za preverjanje Stripe webhook podpisov.

#### Kafka
- `Kafka:EnableKafka` — vklopi objavo dogodkov (Outbox publisher).
- `Kafka:BootstrapServers` — Kafka broker.
- `Kafka:ClientId` — client id za Kafka odjemalca.
- `Kafka:PaymentsTopic` — topic za payment dogodke.
- `Kafka:SecurityProtocol` — protokol (`Plaintext`).
- `Kafka:SaslMechanism` — SASL mehanizem (`ScramSha256`).
- `Kafka:SaslUsername` — SASL uporabnik.
- `Kafka:SaslPassword` — SASL geslo.

#### Swagger
- `SwaggerPrefix` — javna predpona (npr. `/payment`) za pravilne Swagger URL-je.

#### Hosting
- `AllowedHosts` — dovoljeni hosti.

## Lokalno testiranje Stripe

### 1) Predpogoji

- Zagnan `payment-service` lokalno (primer: `https://localhost:8007`)
- Stripe test ključ (`sk_test_...`)
- Endpoint za webhooke v servisu:
  - `POST https://localhost:8007/webhooks/stripe`

> Če lokalno nimaš HTTPS, uporabi `http://localhost:8007/...` tudi v Stripe CLI ukazu.

---

### 2) Nastavi Stripe ApiKey v konfiguracijo

V `appsettings.Development.json` ali prek env var:

#### appsettings
```json
{
  "Stripe": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": ""
  }
}
```

#### env var
```bash
Stripe__ApiKey=sk_test_...
```

---

### 3) Namestitev Stripe CLI (Windows prek Scoop)

```powershell
# 1) Namesti Scoop (če ga še nimaš)
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
irm get.scoop.sh | iex

# 2) Namesti Stripe CLI
scoop bucket add stripe https://github.com/stripe/scoop-stripe-cli.git
scoop install stripe
```

---


### 4) Forward webhookov na lokalni endpoint

Začni poslušanje Stripe dogodkov in jih preusmeri na tvoj lokalni servis:

```bash
stripe listen --forward-to https://localhost:8007/webhooks/stripe
```

- Stripe CLI bo izpisal **webhook signing secret** v obliki `whsec_...`
- Ta secret kopiraj v konfiguracijo servisa kot `Stripe:WebhookSecret`


> Če servis že teče, ga po spremembi konfiguracije restartaj.

---

### 5) Testiranje webhookov

#### 6.1 Pošlji testni dogodek (Stripe CLI)
Lahko sprožiš testni event direktno iz CLI:

```bash
stripe trigger payment_intent.succeeded
```

#### 6.2 Testiranje plačila s testno kartico (če imaš UI / endpoint za create intent)
Uporabi testno kartico:
- `4242 4242 4242 4242`

---

## Lokalno testiranje Kafka

---


### 1) Konfiguracija

V `appsettings.Development.json` (ali env var) nastavi Kafka.

#### Minimalna konfiguracija (lokalna Kafka brez auth)
```json
{
  "Kafka": {
    "EnableKafka": true,
    "BootstrapServers": "localhost:9092",
    "ClientId": "payment-service",
    "PaymentsTopic": "booking.payments",
    "SecurityProtocol": "",
    "SaslMechanism": "",
    "SaslUsername": "",
    "SaslPassword": ""
  }
}
```

#### Env var ekvivalenti
```bash
Kafka__EnableKafka=true
Kafka__BootstrapServers=localhost:9092
Kafka__ClientId=payment-service
Kafka__PaymentsTopic=booking.payments
```

---

### 2) Zagon lokalne Kafka (priporočeno: Redpanda)

```bash
docker run -d --name redpanda \
  -p 9092:9092 -p 9644:9644 \
  redpanda/redpanda:latest \
  redpanda start --overprovisioned --smp 1 --memory 1G --reserve-memory 0M \
  --node-id 0 --check=false \
  --kafka-addr PLAINTEXT://0.0.0.0:9092 \
  --advertise-kafka-addr PLAINTEXT://localhost:9092
```

### 3) Zagon servisa z omogočeno Kafka

Pričakovano:
- če je `Kafka:EnableKafka=true`, se zažene `OutboxPublisherService`
- ob akciji, ki spremeni stanje plačila, se zapiše Outbox zapis in nato objavi na `Kafka:PaymentsTopic`

---

### 4) Kafka UI

 Za pregled topicov in sporočil lahko **Kafka UI** v Dockerju.

```bash
docker run -d --name kafka-ui \
  -p 8081:8080 \
  -e KAFKA_CLUSTERS_0_NAME=local \
  -e KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS=host.docker.internal:9092 \
  provectuslabs/kafka-ui:latest
```

---

