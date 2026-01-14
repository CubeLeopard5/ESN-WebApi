# Affichage des Inscrits aux Événements (Section Admin)

**Date** : 2026-01-12
**Auteur** : Claude + Utilisateur
**Type** : Feature
**Statut** : ✅ Implémenté (simplifié - sans modal réponses)

---

## 📋 Contexte et Objectif

### Problème/Besoin

Actuellement, il n'existe pas de page dans la section administrateur permettant de visualiser facilement les personnes inscrites à chaque événement. Les administrateurs et membres ESN ont besoin de :
- Voir rapidement qui est inscrit à un événement
- Consulter les réponses aux formulaires d'inscription
- Exporter ou gérer les inscriptions
- Suivre le taux de remplissage des événements

### Objectif

Créer une page dans la section administration (`/admin/events/registered` ou `/admin/events/{id}/registrations`) permettant de :
1. Afficher la liste de tous les événements avec leur nombre d'inscrits
2. Visualiser en détail les inscrits d'un événement spécifique
3. Voir les informations des participants (nom, prénom, email)
4. Consulter les réponses aux formulaires SurveyJS
5. Afficher des statistiques (taux de remplissage, etc.)

### Impact

- **Utilisateurs affectés** : Administrateurs et membres ESN uniquement
- **Modules impactés** :
  - Frontend : Nouvelle page admin (`/admin/events/registrations` ou `/admin/events/{id}/registrations`)
  - Backend : Endpoint existant `GET /api/Events/{id}/registrations` (déjà implémenté ✅)
- **Breaking changes** : Non

---

## 🎯 Spécifications Fonctionnelles

### User Stories / Cas d'Usage

1. **En tant que membre ESN**, je veux voir la liste de tous les événements avec le nombre d'inscrits afin de suivre la popularité des événements
   - Affichage : Liste des événements avec titre, date, inscrits/capacité max
   - Tri : Par date décroissante (événements récents en premier)
   - Filtrage : Recherche par titre d'événement

2. **En tant que membre ESN**, je veux sélectionner un événement et voir tous les inscrits afin de gérer les participants
   - Affichage : Table avec nom, prénom, email, date d'inscription, statut
   - Actions : Voir les détails des réponses au formulaire

3. **En tant que membre ESN**, je veux voir les réponses au formulaire SurveyJS de chaque inscrit afin de mieux organiser l'événement
   - Affichage : Modal ou section avec les réponses formatées
   - Format : Questions/Réponses lisibles

4. **En tant que membre ESN**, je veux voir des statistiques sur l'événement afin d'évaluer son succès
   - Taux de remplissage : X/Y inscrits (pourcentage)
   - Date limite d'inscription
   - Nombre d'inscriptions par jour (graphique optionnel)

### Règles Métier

- **Autorisation** : Accessible uniquement aux utilisateurs avec `studentType === "esn_member"` ou rôle `Admin`
- **Statut inscription** : Afficher uniquement les inscriptions avec `status === "Registered"`
- **Données sensibles** : Ne pas afficher les mots de passe ou tokens
- **Ordre d'affichage** : Inscrits triés par date d'inscription (plus récents en premier)
- **Événements passés** : Afficher tous les événements (passés et futurs)

### Comportement Attendu

#### Page Liste des Événements
1. Navigation depuis le menu admin vers `/admin/events/registrations`
2. Affichage d'une table avec colonnes :
   - Titre de l'événement
   - Date de l'événement (StartDate)
   - Lieu
   - Inscrits (X/Y) avec badge coloré selon le taux :
     - Vert : < 70% de capacité
     - Orange : 70-90% de capacité
     - Rouge : > 90% de capacité
   - Actions : Bouton "Voir les inscrits"
3. Barre de recherche pour filtrer par titre
4. Pagination (10 événements par page)

#### Page Détails des Inscrits
1. Navigation depuis la liste ou directement via `/admin/events/{id}/registrations`
2. En-tête avec informations de l'événement :
   - Titre, description, date, lieu
   - Taux de remplissage : badge avec X/Y inscrits
   - Dates d'inscription (registration period)
3. Table des inscrits avec colonnes :
   - Nom complet (firstName + lastName)
   - Email
   - Date d'inscription (registeredAt)
   - Statut (badge)
   - Actions : Bouton "Voir réponses"
4. Modal des réponses au formulaire :
   - Affichage formaté JSON → Questions/Réponses lisibles
   - Parse du SurveyJsData pour afficher proprement

### Cas Limites

- **Événement sans inscrits** : Afficher message "Aucun inscrit pour cet événement"
- **Événement inexistant** : Redirection vers liste + toast d'erreur "Événement non trouvé"
- **SurveyJsData vide ou null** : Afficher "Aucune réponse au formulaire"
- **SurveyJsData invalide (JSON mal formé)** : Afficher message d'erreur + JSON brut
- **Utilisateur non autorisé** : Middleware `admin` redirige vers `/login`
- **Événement passé** : Affichage normal (pas de restriction)

---

## 🏗️ Conception Technique

### Architecture

#### Couches Impactées

- [x] **Web (Backend)** : Endpoint existant ✅ `GET /api/Events/{id}/registrations`
- [ ] **Business** : Service existant ✅ `GetEventRegistrationsAsync()`
- [ ] **Dal** : Repository existant ✅
- [ ] **Dto** : DTO existant ✅ `EventWithRegistrationsDto`, `EventRegistrationDto`
- [ ] **Bo** : Entité existante ✅
- [x] **Frontend** : Nouvelle page `/admin/events/registrations` et `/admin/events/{id}/registrations`

**Conclusion** : Le backend est déjà complet. Cette feature est **uniquement frontend**.

#### Diagramme de Flux

```
┌──────────────┐
│  Admin Menu  │
│              │
└──────┬───────┘
       │ Navigate to /admin/events/registrations
       ▼
┌──────────────────────────────────────┐
│  Page: Liste des Événements          │
│  - GET /api/Events (pagination)      │
│  - Affiche: Title, Date, Inscrits/Max│
│  - Recherche, Pagination             │
└──────┬───────────────────────────────┘
       │ Click "Voir les inscrits" (eventId)
       ▼
┌──────────────────────────────────────┐
│  Page: Détails Inscrits Événement   │
│  - GET /api/Events/{id}/registrations│
│  - Affiche: EventWithRegistrationsDto│
│  - Table: Registrations[]            │
└──────┬───────────────────────────────┘
       │ Click "Voir réponses" (registration)
       ▼
┌──────────────────────────────────────┐
│  Modal: Réponses au Formulaire       │
│  - Parse SurveyJsData JSON           │
│  - Affiche: Questions → Réponses     │
└──────────────────────────────────────┘
```

### Interfaces Publiques

#### API Endpoints (Backend - Existant ✅)

```csharp
/// <summary>
/// Récupère tous les inscrits d'un événement avec leurs réponses
/// </summary>
/// <param name="id">ID de l'événement</param>
/// <returns>Événement avec liste des inscrits</returns>
[Authorize]
[HttpGet("{id}/registrations")]
[ProducesResponseType(typeof(EventWithRegistrationsDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<EventWithRegistrationsDto>> GetEventRegistrations(int id)
{
    // Implémentation existante
}
```

#### Frontend Composables (Nouveau)

**Déjà existant** dans `useEventApi.ts` :
```typescript
const getEventWithRegistrations = async (id: number): Promise<EventWithRegistrationsDto> => {
    return await api.get(`/Events/${id}/registrations`) as EventWithRegistrationsDto;
}
```

**Pas besoin de créer de nouveau composable** ✅

#### Frontend Pages (Nouveau)

**1. Liste des événements avec inscrits** :
- Fichier : `app/pages/admin/events/registrations.vue`
- Route : `/admin/events/registrations`
- Middleware : `['auth', 'admin']`
- Layout : `administration`

**2. Détails des inscrits d'un événement** :
- Fichier : `app/pages/admin/events/[id]/registrations.vue`
- Route : `/admin/events/{id}/registrations`
- Middleware : `['auth', 'admin']`
- Layout : `administration`

### Modèles de Données

#### DTOs (Backend - Existant ✅)

```csharp
/// <summary>
/// Événement avec liste complète des inscrits
/// </summary>
public class EventWithRegistrationsDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxParticipants { get; set; }
    public string? EventfrogLink { get; set; }
    public string? SurveyJsData { get; set; }
    public DateTime? CreatedAt { get; set; }
    public UserDto? Organizer { get; set; }
    public List<EventRegistrationDto> Registrations { get; set; }
    public int TotalRegistered { get; set; }
}

/// <summary>
/// Inscription d'un utilisateur à un événement
/// </summary>
public class EventRegistrationDto
{
    public int Id { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string Status { get; set; } // "Registered", "Cancelled"
    public string SurveyJsData { get; set; } // Réponses JSON
    public UserDto User { get; set; }
}
```

#### Types Frontend (Existant ✅)

```typescript
// app/types/event.ts
export interface EventWithRegistrationsDto {
    id: number
    title: string
    description?: string
    location?: string
    startDate: string
    endDate?: string
    maxParticipants?: number
    eventfrogLink?: string
    surveyJsData?: string
    createdAt?: string
    organizer?: UserDto
    registrations: EventRegistrationDto[]
    totalRegistered: number
}

export interface EventRegistrationDto {
    id: number
    registeredAt?: string
    status: string
    surveyJsData: string
    user: UserDto
}
```

#### Validation

**Backend** : Aucune validation nécessaire (lecture seule)

**Frontend** :
- Vérifier `studentType === "esn_member"` via middleware `admin`
- Parser le JSON `surveyJsData` avec try-catch

### Flux de Données

#### Page Liste des Événements

1. **Montage du composant** : `onMounted()`
2. **Appel API** : `getAllEvents({ pageNumber: 1, pageSize: 10 })`
3. **Affichage table** : Itération sur `events.items[]`
4. **Calcul badges** :
   ```typescript
   const fillRate = computed((event: EventDto) => {
       if (!event.maxParticipants) return 0;
       return (event.registeredCount / event.maxParticipants) * 100;
   });

   const badgeColor = computed((rate: number) => {
       if (rate < 70) return 'green';
       if (rate < 90) return 'orange';
       return 'red';
   });
   ```
5. **Navigation** : `router.push(\`/admin/events/\${eventId}/registrations\`)`

#### Page Détails Inscrits

1. **Montage du composant** : `onMounted()`
2. **Extraction ID** : `const eventId = route.params.id`
3. **Appel API** : `getEventWithRegistrations(eventId)`
4. **Affichage données** :
   - En-tête : `eventData.title`, `eventData.location`, etc.
   - Taux : `eventData.totalRegistered / eventData.maxParticipants`
   - Table : Itération sur `eventData.registrations[]`
5. **Modal réponses** :
   - Click "Voir réponses" → `showModal(registration)`
   - Parse JSON : `JSON.parse(registration.surveyJsData)`
   - Affichage formaté :
     ```typescript
     const formattedAnswers = computed(() => {
         try {
             const answers = JSON.parse(selectedRegistration.value.surveyJsData);
             return Object.entries(answers).map(([key, value]) => ({
                 question: key,
                 answer: value
             }));
         } catch {
             return null;
         }
     });
     ```

### Dépendances

- **Packages NuGet** : Aucun (backend complet)
- **Packages NPM** : Aucun (composants Nuxt UI existants)
- **Services externes** : Aucun
- **Migrations DB** : Aucune (structure existante)

---

## 🔒 Sécurité

### Authentification & Autorisation

- **Rôles requis** : `studentType === "esn_member"` OU rôle `Admin`
- **Middleware** : `['auth', 'admin']` sur toutes les pages
- **Claims nécessaires** : Token JWT valide avec `studentType` claim
- **Endpoints publics** : Non (requiert `[Authorize]`)

### Validation des Données

**Backend** :
- Validation `id` via `[ValidateId]` middleware
- Vérification existence événement dans le service

**Frontend** :
- Validation `eventId` numérique
- Try-catch sur parse JSON `surveyJsData`
- Validation token JWT avant affichage

### Protection Contre les Vulnérabilités

- [x] Injection SQL : ✅ EF Core paramétré
- [x] XSS : ✅ Validation et encodage automatique Vue/Nuxt
- [x] CSRF : ✅ Tokens anti-CSRF si nécessaire
- [x] Exposition de données : ✅ Pas de mots de passe dans EventRegistrationDto

### Audit et Logging

**Backend** :
- Log `GetEventRegistrations` appelé (niveau Information)
- Log si événement non trouvé (niveau Warning)

**Frontend** :
- Console.error si échec chargement données
- Toast notifications pour erreurs utilisateur

---

## 🧪 Stratégie de Tests

### Tests Unitaires

#### Backend (Déjà testé ✅)

Endpoint `GetEventRegistrations` et service `GetEventRegistrationsAsync` déjà testés dans :
- `Tests/Controllers/EventsControllerTests.cs`
- `Tests/Services/EventServiceTests.cs`

**Pas de nouveaux tests backend nécessaires** ✅

#### Frontend (Nouveau)

Tests E2E Playwright recommandés :

```typescript
// tests/e2e/admin-registrations.spec.ts
test('Admin can view event registrations', async ({ page }) => {
    // Login as ESN member
    await page.goto('/login');
    await page.fill('input[name="email"]', 'esn@test.com');
    await page.fill('input[name="password"]', 'password');
    await page.click('button[type="submit"]');

    // Navigate to registrations page
    await page.goto('/admin/events/registrations');
    await expect(page.locator('h1')).toContainText('Inscriptions aux Événements');

    // Click on first event
    await page.click('button:has-text("Voir les inscrits")').first();

    // Verify registrations table
    await expect(page.locator('table')).toBeVisible();
    await expect(page.locator('thead th')).toContainText('Nom');
});

test('Admin can view registration survey answers', async ({ page }) => {
    // Setup...
    await page.goto('/admin/events/1/registrations');

    // Open modal
    await page.click('button:has-text("Voir réponses")').first();

    // Verify modal content
    await expect(page.locator('.modal')).toBeVisible();
    await expect(page.locator('.modal')).toContainText('Réponses au formulaire');
});
```

### Scénarios à Tester

#### Cas Nominaux (Happy Path)
- [ ] Affichage liste événements avec nombre d'inscrits
- [ ] Navigation vers page détails inscrits
- [ ] Affichage table des inscrits
- [ ] Ouverture modal réponses formulaire
- [ ] Parse et affichage JSON réponses
- [ ] Calcul et affichage taux de remplissage
- [ ] Badges colorés selon taux

#### Cas d'Erreur
- [ ] Événement inexistant → toast erreur + redirection
- [ ] API indisponible → toast erreur
- [ ] JSON surveyJsData invalide → affichage message d'erreur
- [ ] Utilisateur non autorisé → middleware redirige vers /login

#### Cas Limites
- [ ] Événement sans inscrits → message "Aucun inscrit"
- [ ] SurveyJsData null → message "Aucune réponse"
- [ ] Événement passé → affichage normal
- [ ] MaxParticipants null → taux non calculable (afficher "Illimité")
- [ ] Liste vide d'événements → message approprié

### Couverture Cible

**Backend** : 100% (déjà atteint ✅)
**Frontend** : Tests E2E couvrant les scénarios principaux

---

## 📦 Plan d'Implémentation

### Étapes d'Implémentation

1. [x] **Backend déjà complet** ✅
   - Endpoint `GET /api/Events/{id}/registrations` existe
   - Service `GetEventRegistrationsAsync()` existe
   - DTOs `EventWithRegistrationsDto` et `EventRegistrationDto` existent

2. [ ] **Créer page liste événements** : `app/pages/admin/events/registrations.vue`
   - Import composables : `useEventApi()`, `useRouter()`, `useToast()`, `useFormatDate()`
   - État : `events` (PagedResult<EventDto>), `isLoading`, `searchQuery`
   - Fonctions : `loadEvents()`, `navigateToRegistrations(eventId)`
   - Template : Table UTable avec colonnes (Title, Date, Location, Inscrits/Max, Actions)
   - Badges colorés pour taux de remplissage

3. [ ] **Créer page détails inscrits** : `app/pages/admin/events/[id]/registrations.vue`
   - Import composables : `useEventApi()`, `useRoute()`, `useRouter()`, `useToast()`, `useFormatDate()`
   - État : `eventData` (EventWithRegistrationsDto), `isLoading`, `selectedRegistration`, `showModal`
   - Fonctions : `loadRegistrations()`, `viewAnswers(registration)`, `parseAnswers(surveyJsData)`
   - Template :
     - En-tête : Infos événement + taux de remplissage
     - Table : Inscrits avec colonnes (Nom, Email, Date inscription, Statut, Actions)
     - Modal : Affichage réponses formatées

4. [ ] **Ajouter lien dans menu admin**
   - Fichier : `app/layouts/administration.vue`
   - Ajouter item dans `navigationItems` :
     ```vue
     {
         label: 'Inscriptions Événements',
         icon: 'i-heroicons-user-group',
         to: '/admin/events/registrations'
     }
     ```

5. [ ] **Créer composant réutilisable (optionnel)** : `app/components/admin/RegistrationAnswersModal.vue`
   - Props : `registration` (EventRegistrationDto), `isOpen` (boolean)
   - Emit : `close`
   - Template : Modal avec affichage formaté des réponses

6. [ ] **Tester manuellement**
   - Vérifier navigation menu → page liste
   - Vérifier affichage événements
   - Vérifier navigation vers détails
   - Vérifier affichage inscrits
   - Vérifier ouverture modal réponses
   - Tester cas limites (aucun inscrit, JSON invalide, etc.)

7. [ ] **Tests E2E Playwright** (optionnel mais recommandé)
   - Créer fichier `tests/e2e/admin-registrations.spec.ts`
   - Tester scénarios principaux

### Fichiers à Créer/Modifier

#### Nouveau Fichiers

- [ ] `app/pages/admin/events/registrations.vue` - Liste événements avec inscrits
- [ ] `app/pages/admin/events/[id]/registrations.vue` - Détails inscrits événement
- [ ] `app/components/admin/RegistrationAnswersModal.vue` (optionnel) - Modal réponses
- [ ] `tests/e2e/admin-registrations.spec.ts` (optionnel) - Tests E2E

#### Fichiers Modifiés

- [ ] `app/layouts/administration.vue` - Ajouter lien menu
- [ ] `app/types/event.ts` - Vérifier types existants (déjà OK normalement ✅)

### Ordre de Dépendance

```
1. Backend ✅ (déjà complet)
   ↓
2. Page liste événements (app/pages/admin/events/registrations.vue)
   ↓
3. Page détails inscrits (app/pages/admin/events/[id]/registrations.vue)
   ↓
4. Ajouter lien menu (app/layouts/administration.vue)
   ↓
5. Tests E2E (optionnel)
```

**Pas de blocage** : Toutes les étapes frontend peuvent être faites séquentiellement.

---

## 🚀 Déploiement

### Prérequis

- ✅ Nuxt 4.x installé
- ✅ Backend ESN-WebApi en cours d'exécution
- ✅ Compte utilisateur avec `studentType = "esn_member"`

### Migrations de Base de Données

**Aucune migration nécessaire** ✅ (structure existante)

### Configuration

**Aucune configuration supplémentaire nécessaire** ✅

### Ordre de Déploiement

1. Frontend : Déployer les nouvelles pages Vue
2. Vérifier que le backend est accessible
3. Tester avec compte ESN member

---

## 📚 Documentation à Mettre à Jour

- [ ] README.md - Ajouter section "Gestion des inscriptions admin"
- [ ] Screenshots (optionnel) - Capturer interface admin
- [ ] Guide utilisateur admin (optionnel)

---

## ✅ Checklist de Validation

### Avant Implémentation

- [x] Backend vérifié (endpoints, services, DTOs) ✅
- [x] Types frontend vérifiés ✅
- [x] Composables vérifiés ✅
- [x] Architecture respecte séparation en couches ✅
- [x] Sécurité prise en compte (middleware admin) ✅
- [x] Stratégie de tests définie ✅
- [ ] Utilisateur a validé l'approche ⏳

### Après Implémentation

- [ ] Code suit conventions Vue 3 Composition API
- [ ] Middleware `admin` appliqué sur toutes les pages
- [ ] Pas de warnings du compilateur
- [ ] Composants réutilisables créés si nécessaire
- [ ] Tests E2E passent (si implémentés)
- [ ] Navigation fluide (pas de bugs)
- [ ] Toast notifications appropriées
- [ ] Gestion erreurs complète
- [ ] Documentation mise à jour

---

## 🎯 Critères d'Acceptation

### Fonctionnels

- [ ] La page `/admin/events/registrations` affiche tous les événements avec nombre d'inscrits
- [ ] Le taux de remplissage est affiché avec badge coloré (vert/orange/rouge)
- [ ] Click "Voir les inscrits" navigue vers `/admin/events/{id}/registrations`
- [ ] La page détails affiche tous les inscrits avec nom, email, date, statut
- [ ] Click "Voir réponses" ouvre un modal avec les réponses formatées
- [ ] Les événements sans inscrits affichent "Aucun inscrit"
- [ ] Les JSON invalides affichent un message d'erreur approprié
- [ ] Uniquement accessible aux membres ESN (middleware admin)

### Techniques

- [ ] Code respecte conventions Vue 3 Composition API
- [ ] Composables existants réutilisés
- [ ] Pas de duplication de code
- [ ] Gestion d'erreurs complète (try-catch, toast)
- [ ] Loading states affichés
- [ ] Responsive design (mobile-friendly)
- [ ] Dark mode supporté

### Non-Fonctionnels

- [ ] Performance : Chargement < 2s
- [ ] Sécurité : Middleware admin vérifié
- [ ] UX : Navigation intuitive
- [ ] Logs : Erreurs loggées en console
- [ ] Accessibilité : Labels appropriés

---

## 📝 Notes et Décisions

### Décisions de Conception

1. **Deux pages séparées** : Liste événements + Détails inscrits
   - **Pourquoi** : Séparation des responsabilités, meilleure UX
   - **Alternative** : Une seule page avec accordéon (rejetée, moins claire)

2. **Modal pour réponses formulaire** :
   - **Pourquoi** : Évite surcharge visuelle, focus sur les réponses
   - **Alternative** : Section expandable (pourrait être envisagée)

3. **Réutilisation endpoint existant** :
   - **Pourquoi** : Backend déjà complet, pas besoin de modifier
   - **Alternative** : Créer nouvel endpoint admin (inutile)

4. **Middleware admin** :
   - **Pourquoi** : Restriction accès, sécurité
   - **Alternative** : Vérification manuelle dans composant (moins sûr)

### Alternatives Considérées

1. **Page unique avec tabs** : Liste + Détails dans tabs
   - **Rejetée** : Moins intuitif, complexifie le code

2. **Endpoint séparé `/admin/events/registrations`** :
   - **Rejetée** : Endpoint existant suffit

3. **Export CSV des inscrits** :
   - **Report à plus tard** : Feature supplémentaire, pas critique

### Points d'Attention

- **Parse JSON surveyJsData** : Gérer les cas où JSON est invalide ou vide
- **Performance** : Si beaucoup d'inscrits (100+), considérer pagination des inscrits
- **Modal accessibility** : S'assurer que le modal est accessible (focus trap, ESC key)

### Questions Ouvertes

- **Export CSV** : Faut-il ajouter un bouton d'export des inscrits en CSV ?
- **Filtrage/Tri** : Faut-il ajouter filtres sur table inscrits (par nom, date, etc.) ?
- **Graphiques** : Faut-il ajouter un graphique d'évolution des inscriptions ?

---

## 📊 Suivi

| Date | Statut | Commentaire |
|------|--------|-------------|
| 2026-01-12 | 🟡 En Documentation | Création du document de spec |
| 2026-01-12 | 🟢 Implémenté | Feature implémentée et testée |
| 2026-01-12 | ✅ Simplifié | Retrait du modal des réponses au formulaire sur demande utilisateur |

---

## 🔄 Modifications Post-Implémentation

### Simplification Demandée (2026-01-12)

**Changement** : Retrait de la fonctionnalité "Réponses au formulaire"

**Éléments retirés** :
- Modal d'affichage des réponses SurveyJS
- Colonne "Actions" avec bouton "Voir réponses"
- Fonctions de parsing des réponses JSON

**Raison** : Simplification de l'interface demandée par l'utilisateur

**Version finale** :
- Page liste des événements avec nombre d'inscrits ✅
- Page détails affichant : Nom, Email, Date d'inscription, Statut ✅
- Pas de modal de réponses ✅

---

**Ce document suit le template standard du projet ESN-WebApi**
**Backend déjà complet ✅ - Feature principalement frontend**
