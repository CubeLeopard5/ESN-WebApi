# Documentation des Endpoints API

## Base URL

```
http://localhost:5000/api
https://your-domain.com/api
```

## Authentification

La plupart des endpoints nécessitent un token JWT dans le header Authorization:

```
Authorization: Bearer <votre_token_jwt>
```

Le token est obtenu via l'endpoint `/users/login` et a une durée de validité de 30 minutes.

## Codes de Statut HTTP

- `200 OK` - Requête réussie
- `201 Created` - Ressource créée avec succès
- `400 Bad Request` - Données invalides
- `401 Unauthorized` - Authentification requise ou token invalide
- `403 Forbidden` - Accès interdit (permissions insuffisantes)
- `404 Not Found` - Ressource introuvable
- `409 Conflict` - Ressource déjà existante
- `429 Too Many Requests` - Rate limit dépassé
- `500 Internal Server Error` - Erreur serveur

## Format de Réponse

### Succès
Retourne directement les données ou un objet paginé.

### Erreur
```json
{
  "statusCode": 400,
  "message": "Description de l'erreur",
  "details": "Détails supplémentaires (Dev uniquement)",
  "path": "/api/users/123"
}
```

### Pagination
```json
{
  "items": [...],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

---

## Utilisateurs (`/api/users`)

### Authentification

#### `POST /users/login`
Authentification et obtention d'un token JWT

**Rate Limiting:** 5 requêtes / 5 minutes

**Body:**
```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

**Réponse:** `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "birthDate": "2000-01-01",
    "phoneNumber": "+33123456789",
    "esnCardNumber": "ESN12345",
    "universityName": "Université Paris",
    "studentType": "exchange",
    "transportPass": "Navigo"
  }
}
```

**Erreurs:**
- `401 Unauthorized` - Credentials invalides

---

#### `POST /users/refresh`
Rafraîchissement du token JWT

**Body:**
```json
{
  "token": "ancien_token_expiré"
}
```

**Réponse:** `200 OK`
```json
{
  "token": "nouveau_token",
  "user": { ... }
}
```

**Erreurs:**
- `401 Unauthorized` - Token invalide ou trop ancien (> 7 jours)

---

### Gestion des Utilisateurs

#### `POST /users`
Création d'un nouvel utilisateur

**Rate Limiting:** 3 requêtes / heure

**Body:**
```json
{
  "email": "newuser@example.com",
  "password": "SecurePassword123",
  "firstName": "Jane",
  "lastName": "Smith",
  "birthDate": "1995-06-15",
  "phoneNumber": "+33987654321",
  "esnCardNumber": "ESN54321",
  "universityName": "Sorbonne",
  "studentType": "local",
  "transportPass": "Imagine R"
}
```

**Réponse:** `201 Created`
```json
{
  "id": 2,
  "email": "newuser@example.com",
  ...
}
```

**Erreurs:**
- `400 Bad Request` - Données invalides
- `409 Conflict` - Email déjà utilisé

---

#### `GET /users` 🔒
Liste paginée des utilisateurs

**Query Parameters:**
- `pageNumber` (default: 1)
- `pageSize` (default: 10, max: 100)

**Réponse:** `200 OK`
```json
{
  "items": [
    { "id": 1, "email": "user1@example.com", ... },
    { "id": 2, "email": "user2@example.com", ... }
  ],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

---

#### `GET /users/{id}` 🔒
Détails d'un utilisateur

**Réponse:** `200 OK`
```json
{
  "id": 1,
  "email": "user@example.com",
  "firstName": "John",
  ...
}
```

**Erreurs:**
- `404 Not Found` - Utilisateur introuvable

---

#### `GET /users/me` 🔒
Profil de l'utilisateur connecté

**Réponse:** `200 OK`
```json
{
  "id": 1,
  "email": "user@example.com",
  ...
}
```

---

#### `GET /users/esn-members` 🔒
Liste des membres ESN

**Réponse:** `200 OK`
```json
[
  { "id": 3, "email": "member@esn.com", "studentType": "esn_member", ... }
]
```

---

#### `PUT /users/{id}` 🔒
Modification d'un utilisateur

**Restriction:** Seul le propriétaire ou un Admin

**Body:**
```json
{
  "email": "updated@example.com",
  "firstName": "UpdatedName",
  "lastName": "UpdatedLastName",
  "birthDate": "1995-06-15",
  "phoneNumber": "+33123456789",
  "esnCardNumber": "ESN99999",
  "universityName": "New University",
  "transportPass": "New Pass"
}
```

**Réponse:** `200 OK`
```json
{
  "id": 1,
  "email": "updated@example.com",
  ...
}
```

**Erreurs:**
- `403 Forbidden` - Pas autorisé à modifier cet utilisateur
- `404 Not Found` - Utilisateur introuvable

---

#### `PUT /users/Password/{id}` 🔒
Changement de mot de passe

**Body:**
```json
{
  "oldPassword": "OldPassword123",
  "newPassword": "NewSecurePassword456"
}
```

**Réponse:** `200 OK`
```json
{
  "message": "Password updated successfully"
}
```

**Erreurs:**
- `400 Bad Request` - Ancien mot de passe incorrect
- `404 Not Found` - Utilisateur introuvable

---

#### `DELETE /users/{id}` 🔒👑
Suppression d'un utilisateur

**Restriction:** Admin uniquement

**Réponse:** `200 OK`
```json
{
  "message": "User deleted successfully"
}
```

**Erreurs:**
- `403 Forbidden` - Requiert rôle Admin
- `404 Not Found` - Utilisateur introuvable

---

## Événements (`/api/events`)

### Gestion des Événements

#### `POST /events` 🔒
Création d'un événement

**Body:**
```json
{
  "title": "Welcome Party",
  "description": "Party de bienvenue pour les nouveaux étudiants",
  "location": "ESN Office",
  "startDate": "2025-09-01T19:00:00",
  "endDate": "2025-09-01T23:00:00",
  "maxParticipants": 50,
  "eventfrogLink": "https://eventfrog.ch/event123",
  "surveyJsData": "{\"pages\":[{\"name\":\"page1\",\"elements\":[{\"type\":\"text\",\"name\":\"dietaryRestrictions\"}]}]}"
}
```

**Réponse:** `201 Created`
```json
{
  "id": 1,
  "title": "Welcome Party",
  "userId": 1,
  "createdAt": "2025-01-15T10:00:00",
  ...
}
```

---

#### `GET /events`
Liste paginée des événements

**Query Parameters:**
- `pageNumber` (default: 1)
- `pageSize` (default: 10, max: 100)

**Réponse:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "title": "Welcome Party",
      "startDate": "2025-09-01T19:00:00",
      "registeredCount": 23,
      ...
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 2
}
```

---

#### `GET /events/{id}`
Détails d'un événement

**Réponse:** `200 OK`
```json
{
  "id": 1,
  "title": "Welcome Party",
  "description": "...",
  "location": "ESN Office",
  "startDate": "2025-09-01T19:00:00",
  "endDate": "2025-09-01T23:00:00",
  "maxParticipants": 50,
  "registeredCount": 23,
  "eventfrogLink": "https://eventfrog.ch/event123",
  "surveyJsData": "{...}",
  "userId": 1,
  "createdAt": "2025-01-15T10:00:00"
}
```

**Erreurs:**
- `404 Not Found` - Événement introuvable

---

#### `PUT /events/{id}` 🔒
Modification d'un événement

**Restriction:** Seul le créateur

**Body:** (même structure que POST)

**Réponse:** `200 OK`

**Erreurs:**
- `403 Forbidden` - Pas le créateur
- `404 Not Found` - Événement introuvable

---

#### `DELETE /events/{id}` 🔒
Suppression d'un événement

**Restriction:** Seul le créateur

**Réponse:** `200 OK`
```json
{
  "message": "Event deleted successfully"
}
```

**Erreurs:**
- `403 Forbidden` - Pas le créateur
- `404 Not Found` - Événement introuvable

---

### Inscriptions aux Événements

#### `POST /events/{id}/register` 🔒
Inscription à un événement

**Body:**
```json
{
  "surveyJsData": "{\"dietaryRestrictions\":\"Vegetarian\"}"
}
```

**Réponse:** `200 OK`
```json
{
  "message": "Successfully registered for event"
}
```

**Erreurs:**
- `400 Bad Request` - Déjà inscrit ou événement complet
- `404 Not Found` - Événement introuvable

---

#### `DELETE /events/{id}/register` 🔒
Désinscription d'un événement

**Réponse:** `200 OK`
```json
{
  "message": "Successfully unregistered from event"
}
```

**Erreurs:**
- `404 Not Found` - Pas d'inscription trouvée

---

#### `GET /events/{id}/registrations` 🔒
Liste des inscrits à un événement

**Réponse:** `200 OK`
```json
{
  "eventId": 1,
  "eventTitle": "Welcome Party",
  "registrations": [
    {
      "id": 1,
      "userId": 2,
      "user": {
        "id": 2,
        "firstName": "John",
        "lastName": "Doe",
        "email": "john@example.com"
      },
      "surveyJsData": "{...}",
      "registeredAt": "2025-01-15T12:00:00",
      "status": "Registered"
    }
  ],
  "totalRegistrations": 23
}
```

---

### Templates d'Événements

#### `POST /events/templates` 🔒
Création d'un template

**Body:**
```json
{
  "title": "Welcome Party Template",
  "description": "Template pour les soirées de bienvenue",
  "surveyJsData": "{\"pages\":[...]}"
}
```

**Réponse:** `201 Created`

---

#### `GET /events/templates`
Liste paginée des templates

**Query Parameters:**
- `pageNumber`, `pageSize`

**Réponse:** `200 OK` (format paginé)

---

#### `GET /events/templates/{id}`
Détails d'un template

**Réponse:** `200 OK`

---

#### `PUT /events/templates/{id}` 🔒
Modification d'un template

**Réponse:** `200 OK`

---

#### `DELETE /events/templates/{id}` 🔒
Suppression d'un template

**Réponse:** `200 OK`

---

#### `POST /events/from-template` 🔒
Création d'événement depuis un template

**Body:**
```json
{
  "templateId": 1,
  "title": "Welcome Party September",
  "location": "ESN Office",
  "startDate": "2025-09-01T19:00:00",
  "endDate": "2025-09-01T23:00:00",
  "maxParticipants": 50
}
```

**Réponse:** `201 Created`

---

#### `POST /events/{id}/save-as-template` 🔒
Sauvegarder un événement comme template

**Body:**
```json
{
  "title": "My Event Template",
  "description": "Template basé sur l'événement X"
}
```

**Réponse:** `201 Created`

---

## Calendriers (`/api/calendars`)

#### `POST /calendars` 🔒
Création d'un calendrier

**Body:**
```json
{
  "title": "Welcome Party Calendar Entry",
  "eventDate": "2025-09-01",
  "eventId": 1,
  "mainOrganizerId": 1,
  "eventManagerId": 2,
  "responsableComId": 3,
  "subOrganizerIds": [4, 5, 6]
}
```

**Réponse:** `201 Created`

---

#### `GET /calendars`
Liste paginée des calendriers

**Query Parameters:**
- `pageNumber`, `pageSize`

**Réponse:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "title": "Welcome Party Calendar Entry",
      "eventDate": "2025-09-01",
      "eventId": 1,
      "mainOrganizerId": 1,
      "mainOrganizer": { "id": 1, "firstName": "John", ... },
      "eventManagerId": 2,
      "eventManager": { ... },
      "responsableComId": 3,
      "responsableCom": { ... },
      "subOrganizers": [
        { "id": 4, "firstName": "Alice", ... },
        { "id": 5, "firstName": "Bob", ... }
      ]
    }
  ],
  "totalCount": 20,
  ...
}
```

---

#### `GET /calendars/{id}`
Détails d'un calendrier

**Réponse:** `200 OK`

---

#### `GET /calendars/ByEvent/{eventId}`
Calendriers d'un événement spécifique

**Réponse:** `200 OK`

---

#### `PUT /calendars/{id}` 🔒
Modification d'un calendrier

**Restriction:** Seul l'organisateur principal

**Réponse:** `200 OK`

**Erreurs:**
- `403 Forbidden` - Pas l'organisateur principal

---

#### `DELETE /calendars/{id}` 🔒
Suppression d'un calendrier

**Restriction:** Seul l'organisateur principal

**Réponse:** `200 OK`

---

## Propositions (`/api/propositions`)

#### `POST /propositions` 🔒
Création d'une proposition

**Body:**
```json
{
  "title": "Proposition: Soirée jeux de société",
  "description": "Organiser une soirée mensuelle de jeux de société"
}
```

**Réponse:** `201 Created`
```json
{
  "id": 1,
  "title": "Proposition: Soirée jeux de société",
  "description": "...",
  "userId": 1,
  "votesUp": 0,
  "votesDown": 0,
  "createdAt": "2025-01-15T10:00:00",
  "modifiedAt": "2025-01-15T10:00:00"
}
```

---

#### `GET /propositions`
Liste paginée des propositions (actives uniquement)

**Query Parameters:**
- `pageNumber`, `pageSize`

**Réponse:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "title": "Proposition: Soirée jeux de société",
      "description": "...",
      "userId": 1,
      "user": { "id": 1, "firstName": "John", ... },
      "votesUp": 15,
      "votesDown": 3,
      "createdAt": "2025-01-15T10:00:00"
    }
  ],
  "totalCount": 8,
  ...
}
```

---

#### `GET /propositions/{id}`
Détails d'une proposition

**Réponse:** `200 OK`

**Erreurs:**
- `404 Not Found` - Proposition introuvable ou supprimée

---

#### `PUT /propositions/{id}` 🔒
Modification d'une proposition

**Restriction:** Seul l'auteur

**Body:**
```json
{
  "title": "Titre modifié",
  "description": "Description modifiée"
}
```

**Réponse:** `200 OK`

---

#### `DELETE /propositions/{id}` 🔒
Suppression d'une proposition (soft delete)

**Restriction:** Seul l'auteur

**Réponse:** `200 OK`

---

### Système de Vote

#### `POST /propositions/{id}/vote-up` 🔒
Vote positif

**Rate Limiting:** 30 requêtes / minute

**Réponse:** `200 OK`
```json
{
  "id": 1,
  "votesUp": 16,
  "votesDown": 3,
  ...
}
```

---

#### `POST /propositions/{id}/vote-down` 🔒
Vote négatif

**Rate Limiting:** 30 requêtes / minute

**Réponse:** `200 OK`
```json
{
  "id": 1,
  "votesUp": 15,
  "votesDown": 4,
  ...
}
```

---

## Health Check

#### `GET /health`
Vérification de l'état de santé de l'API

**Réponse:** `200 OK`
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

---

## Légende

🔒 **Authentification requise** - Token JWT obligatoire
👑 **Admin uniquement** - Nécessite le rôle Administrateur

## Exemples d'Utilisation

### Exemple: Flux complet d'inscription à un événement

```bash
# 1. Création de compte
POST /api/users
Body: { "email": "john@example.com", "password": "Secure123", ... }

# 2. Connexion
POST /api/users/login
Body: { "email": "john@example.com", "password": "Secure123" }
Response: { "token": "eyJhbGci...", "user": {...} }

# 3. Consultation des événements
GET /api/events?pageNumber=1&pageSize=10
Headers: Authorization: Bearer eyJhbGci...

# 4. Inscription à un événement
POST /api/events/1/register
Headers: Authorization: Bearer eyJhbGci...
Body: { "surveyJsData": "{\"dietaryRestrictions\":\"None\"}" }

# 5. Vérification de l'inscription
GET /api/events/1
Headers: Authorization: Bearer eyJhbGci...
```

### Exemple: Création et gestion d'un événement

```bash
# 1. Connexion
POST /api/users/login

# 2. Création d'un événement
POST /api/events
Headers: Authorization: Bearer <token>
Body: {
  "title": "Soirée de bienvenue",
  "description": "...",
  "startDate": "2025-09-01T19:00:00",
  "maxParticipants": 50
}

# 3. Création du calendrier associé
POST /api/calendars
Headers: Authorization: Bearer <token>
Body: {
  "title": "Calendrier Soirée",
  "eventDate": "2025-09-01",
  "eventId": 1,
  "mainOrganizerId": 1,
  "subOrganizerIds": [2, 3]
}

# 4. Consultation des inscriptions
GET /api/events/1/registrations
Headers: Authorization: Bearer <token>
```

---

**Note:** Tous les exemples utilisent le format JSON pour les corps de requête et les réponses. Les dates sont au format ISO 8601 (UTC).
