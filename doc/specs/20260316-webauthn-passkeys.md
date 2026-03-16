# WebAuthn / Passkeys - Specification

**Date** : 2026-03-16
**Statut** : Implementé
**Branche** : feature/webauthn-passkeys

---

## Contexte

L'authentification reposait uniquement sur email + password avec JWT. Cette feature ajoute le support **optionnel** des **Passkeys (WebAuthn/FIDO2)** pour permettre une connexion biometrique (Windows Hello, Touch ID, Face ID, cle de securite USB).

**Benefices** :
- Resistance au phishing (credential lie au domaine)
- Meilleure UX (pas de mot de passe a retenir)
- Optionnel pour tous les utilisateurs

---

## Architecture

### Flux Registration (utilisateur connecte)

```
Settings > "Ajouter une cle d'acces"
  → POST /api/passkeys/register/begin     → serveur retourne CredentialCreateOptions
  → navigator.credentials.create()         → navigateur prompt biometrie/PIN
  → POST /api/passkeys/register/complete   → serveur valide et stocke le credential
```

### Flux Login (alternative au mot de passe)

```
Login > "Se connecter avec une cle d'acces"
  → POST /api/passkeys/login/begin         → serveur retourne AssertionOptions
  → navigator.credentials.get()            → navigateur prompt biometrie/PIN
  → POST /api/passkeys/login/complete      → serveur valide, retourne JWT + User
```

### Gestion des passkeys (CRUD)

```
GET    /api/passkeys         → Liste les passkeys de l'utilisateur connecte
PUT    /api/passkeys/{id}    → Renomme une passkey (verification proprietaire)
DELETE /api/passkeys/{id}    → Supprime une passkey (verification proprietaire)
```

---

## Endpoints API

| Methode | Route | Auth | Rate Limit | Description |
|---------|-------|------|------------|-------------|
| POST | `/api/passkeys/register/begin` | Authorize | - | Initie l'enregistrement |
| POST | `/api/passkeys/register/complete` | Authorize | - | Complete l'enregistrement |
| POST | `/api/passkeys/login/begin` | Anonymous | login (5/5min) | Initie le login |
| POST | `/api/passkeys/login/complete` | Anonymous | login (5/5min) | Complete le login |
| GET | `/api/passkeys` | Authorize | - | Liste ses passkeys |
| PUT | `/api/passkeys/{id}` | Authorize | - | Renomme une passkey |
| DELETE | `/api/passkeys/{id}` | Authorize | - | Supprime une passkey |

---

## Modele de donnees

### Entite UserPasskeyBo

| Champ | Type | Description |
|-------|------|-------------|
| Id | int (PK) | Identifiant auto-incremente |
| UserId | int (FK) | Reference vers Users, cascade delete |
| CredentialId | string (512, unique) | Identifiant WebAuthn du credential (base64url) |
| PublicKey | byte[] | Cle publique COSE du credential |
| SignCount | uint | Compteur de signatures (detection clonage) |
| AaGuid | Guid | AAGUID de l'authenticator |
| CredentialType | string (32) | Type de credential ("public-key") |
| DisplayName | string (255, nullable) | Nom d'affichage utilisateur |
| CreatedAt | datetime | Date de creation |
| LastUsedAt | datetime (nullable) | Derniere utilisation |

**Index** : Unique sur `CredentialId`
**FK** : `UserId` → `Users.Id` (CASCADE DELETE)

---

## Securite

| Aspect | Implementation |
|--------|---------------|
| Attestation | `none` (maximise compatibilite) |
| User verification | `preferred` (biometrie si dispo, sinon PIN) |
| Resident key | `preferred` (passkey discoverable si supporte) |
| Challenge | Single-use, TTL 5 min via IMemoryCache |
| Sign count | Verifie et mis a jour a chaque login |
| Status check | Pending/Rejected bloques (comme le login password) |
| Rate limiting | Reutilise la policy `login` (5 tentatives / 5 min) |
| Ownership | Verification userId sur update/delete passkey |
| CredentialId | Index unique, excludeCredentials a la registration |

---

## Refactoring JWT

La generation JWT a ete extraite dans un service dedie pour etre reutilisee par les deux modes de login :

- **IJwtTokenService** : Interface avec methode `GenerateToken(UserBo user)`
- **JwtTokenService** : Implementation (extraite de `UserService.GenerateJwtToken`)
- **UserService** : Injecte `IJwtTokenService` au lieu de la methode privee
- **PasskeyService** : Injecte le meme `IJwtTokenService`

---

## Stack technique

### Backend
- **Fido2 v4.0.0** (NuGet) : Librairie FIDO2/WebAuthn pour .NET
- **Fido2.AspNet v4.0.0** : Extension ASP.NET Core (AddFido2)
- **IMemoryCache** : Stockage des challenges (single-use, TTL 5 min)

### Frontend
- **navigator.credentials API** : API WebAuthn native du navigateur
- **useWebAuthn composable** : Wrapper pour l'API browser + conversion des formats
- **usePasskeyApi composable** : Appels API vers les endpoints passkey

---

## Fichiers crees

### Backend (22 fichiers)

| Fichier | Couche | Role |
|---------|--------|------|
| `Business/Interfaces/IJwtTokenService.cs` | Business | Interface generation JWT |
| `Business/Auth/JwtTokenService.cs` | Business | Implementation generation JWT |
| `Business/Interfaces/IPasskeyService.cs` | Business | Interface service passkey |
| `Business/Passkey/PasskeyService.cs` | Business | Implementation service passkey |
| `Bo/Models/UserPasskeyBo.cs` | Bo | Entite passkey |
| `Dto/Passkey/PasskeyDto.cs` | Dto | DTO reponse passkey |
| `Dto/Passkey/PasskeyRegistrationCompleteDto.cs` | Dto | DTO complete registration |
| `Dto/Passkey/PasskeyLoginBeginDto.cs` | Dto | DTO begin login |
| `Dto/Passkey/PasskeyLoginCompleteDto.cs` | Dto | DTO complete login |
| `Dto/Passkey/UpdatePasskeyDto.cs` | Dto | DTO rename passkey |
| `Dal/Repositories/Interfaces/IPasskeyRepository.cs` | Dal | Interface repository |
| `Dal/Repositories/PasskeyRepository.cs` | Dal | Implementation repository |
| `Web/Controllers/PasskeysController.cs` | Web | Controller API |
| `Tests/Services/JwtTokenServiceTests.cs` | Tests | Tests JWT service |
| `Tests/Services/PasskeyServiceTests.cs` | Tests | Tests passkey service |
| `Tests/Controllers/PasskeysControllerTests.cs` | Tests | Tests controller |
| `Tests/Repositories/PasskeyRepositoryTests.cs` | Tests | Tests repository |

### Frontend (5 fichiers)

| Fichier | Role |
|---------|------|
| `app/types/passkey.ts` | Types TypeScript |
| `app/composables/useWebAuthn.ts` | Wrapper API WebAuthn navigateur |
| `app/composables/usePasskeyApi.ts` | Appels API passkey |
| `app/components/login/passkeyLogin.vue` | Bouton login passkey |
| `app/components/users/passkeys.vue` | Gestion passkeys (settings) |

### Fichiers modifies

| Fichier | Modification |
|---------|-------------|
| `Business/Business.csproj` | NuGet Fido2 |
| `Web/Web.csproj` | NuGet Fido2.AspNet |
| `Bo/Models/UserBo.cs` | Navigation property Passkeys |
| `Dal/EsnDevContext.cs` | DbSet + entity config |
| `Dal/UnitOfWork/Interfaces/IUnitOfWork.cs` | IPasskeyRepository |
| `Dal/UnitOfWork/UnitOfWork.cs` | Lazy init PasskeyRepository |
| `Business/User/UserService.cs` | Injecte IJwtTokenService |
| `Web/Program.cs` | DI Fido2 + services + MemoryCache |
| `Web/Mappings/MappingProfile.cs` | Mapping PasskeyBo → PasskeyDto |
| `Web/appsettings.json` | Config Fido2 |
| `Tests/Services/UserServiceTests.cs` | Mock IJwtTokenService |
| `app/composables/useAuth.ts` | loginWithPasskey() |
| `app/components/login/loginForm.vue` | Bouton passkey + separateur |
| `app/pages/users.vue` | Section UsersPasskeys |

---

## Tests

- **34 nouveaux tests** (total 397, 0 echecs)
- **JwtTokenServiceTests** : 3 tests (token valide, avec role, sans role)
- **PasskeyServiceTests** : 11 tests (registration, login, CRUD, ownership, expired challenge)
- **PasskeysControllerTests** : 10 tests (tous les endpoints, erreurs 400/403/404)
- **PasskeyRepositoryTests** : 7 tests (InMemory DB, CRUD, ordering)
- **UserServiceTests** : 3 tests modifies (mock IJwtTokenService)

---

## Notes d'implementation

### Conversion des enums Fido2 → WebAuthn

La librairie Fido2NetLib serialise les enums en PascalCase (`"None"`, `"Preferred"`, `"PublicKey"`) et les algorithmes en strings (`"ES256"`). L'API WebAuthn du navigateur attend du lowercase (`"none"`, `"preferred"`, `"public-key"`) et des identifiants COSE numeriques (`-7` pour ES256).

Le composable `useWebAuthn.ts` effectue cette conversion dans `parseCreationOptions()` et `parseRequestOptions()`.

### Challenge store

Les challenges sont stockes via `IMemoryCache` avec un TTL de 5 minutes. Chaque challenge est single-use : il est retire du cache des qu'il est consomme par le endpoint `complete`. Les cles de cache suivent le format :
- Registration : `passkey:reg:{guid}`
- Login : `passkey:login:{guid}`

### Compatibilite navigateur

Le composable `useWebAuthn` detecte le support via `window.PublicKeyCredential`. Le bouton de login passkey et la section de gestion ne s'affichent que si le navigateur supporte WebAuthn. Les ceremonies annulees par l'utilisateur (`NotAllowedError`) sont gerees silencieusement.
