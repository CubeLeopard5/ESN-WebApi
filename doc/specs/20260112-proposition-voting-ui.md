# Interface de Vote sur les Propositions

**Date** : 2026-01-12
**Auteur** : Claude + Utilisateur
**Type** : Feature (UI/UX)
**Statut** : 🟡 En Documentation

---

## 📋 Contexte et Objectif

### Problème/Besoin

Le système de vote sur les propositions est **entièrement implémenté au backend** (endpoints, logique métier, base de données) mais **n'est pas accessible aux utilisateurs** car aucune interface utilisateur n'existe pour voter.

**État actuel** :
- ✅ Backend : Système de vote complet (VoteUp/VoteDown, rate limiting, contraintes)
- ❌ Frontend : Aucune UI de vote visible
- ❌ Utilisateurs ne peuvent pas voir les votes existants
- ❌ Utilisateurs ne peuvent pas voter sur les propositions

### Objectif

Créer l'interface utilisateur complète pour le système de vote sur les propositions en exploitant les endpoints backend déjà existants :
1. Afficher les compteurs de votes (VotesUp / VotesDown) sur chaque proposition
2. Permettre aux utilisateurs authentifiés de voter (Up ou Down)
3. Afficher l'état du vote personnel de l'utilisateur (a-t-il déjà voté ?)
4. Permettre de changer son vote
5. Fournir un feedback immédiat après chaque action

### Impact

- **Utilisateurs affectés** : Tous les utilisateurs authentifiés
- **Modules impactés** :
  - Frontend : Pages propositions (liste et détails), composants
  - Backend : **Aucune modification nécessaire** (déjà complet)
- **Breaking changes** : Non

---

## 🎯 Spécifications Fonctionnelles

### User Stories / Cas d'Usage

#### US1 : Voir les votes sur une proposition
**En tant qu'utilisateur (authentifié ou non)**, je veux voir le nombre de votes positifs et négatifs sur chaque proposition afin d'évaluer sa popularité.

**Acceptance Criteria** :
- Les compteurs VotesUp et VotesDown sont visibles sur la page de détails
- Les compteurs sont optionnellement visibles sur la liste des propositions
- Format d'affichage clair : icône + nombre (ex: ▲ 42  ▼ 5)
- Score net optionnel : +37 (VotesUp - VotesDown)

#### US2 : Voter pour une proposition
**En tant qu'utilisateur authentifié**, je veux pouvoir voter positivement ou négativement sur une proposition afin d'exprimer mon opinion.

**Acceptance Criteria** :
- Boutons de vote visibles (Upvote ▲ et Downvote ▼)
- Clic sur un bouton envoie la requête au backend
- Feedback immédiat : toast de confirmation ou d'erreur
- Mise à jour automatique des compteurs après vote
- Boutons désactivés pendant le chargement

#### US3 : Voir mon vote personnel
**En tant qu'utilisateur authentifié ayant déjà voté**, je veux voir quel vote j'ai émis afin de connaître ma position.

**Acceptance Criteria** :
- Le bouton correspondant à mon vote est mis en évidence (couleur/style différent)
- Si j'ai voté Up : bouton Up actif, bouton Down inactif
- Si j'ai voté Down : bouton Down actif, bouton Up inactif
- Si je n'ai pas voté : les deux boutons sont neutres

#### US4 : Changer mon vote
**En tant qu'utilisateur authentifié ayant déjà voté**, je veux pouvoir changer mon vote afin de corriger mon opinion.

**Acceptance Criteria** :
- Cliquer sur le bouton opposé change mon vote
- Exemple : j'ai voté Up, je clique Down → mon vote devient Down
- Les compteurs se mettent à jour correctement (Up -1, Down +1)
- Feedback visuel immédiat

#### US5 : Retirer mon vote (optionnel - si implémenté backend)
**En tant qu'utilisateur authentifié ayant déjà voté**, je veux pouvoir annuler mon vote afin de rester neutre.

**Acceptance Criteria** :
- Cliquer sur le même bouton retire mon vote
- Les compteurs se mettent à jour
- Les deux boutons redeviennent neutres
- ⚠️ **À vérifier** : Le backend supporte-t-il cette fonctionnalité ?

### Règles Métier

#### Autorisation
- **Vote** : Uniquement utilisateurs authentifiés
- **Visualisation** : Tous (authentifiés et anonymes)

#### Contraintes Techniques
- ✅ Un utilisateur ne peut voter qu'une fois par proposition (contrainte DB)
- ✅ Rate limiting : 30 votes par minute par IP (déjà implémenté backend)
- ✅ Un utilisateur peut changer son vote autant de fois qu'il veut

#### Comportement du Vote
D'après l'analyse du backend (`PropositionService.VoteAsync`) :
- **Voter Up quand déjà voté Up** : Aucun changement (idempotent)
- **Voter Up quand déjà voté Down** : Change le vote en Up (Up +1, Down -1)
- **Voter Down quand déjà voté Down** : Aucun changement (idempotent)
- **Voter Down quand déjà voté Up** : Change le vote en Down (Up -1, Down +1)

#### États Possibles
| État Actuel | Action | Nouvel État | Backend Response |
|-------------|--------|-------------|------------------|
| Pas de vote | Vote Up | Up | 200 OK, VotesUp +1 |
| Pas de vote | Vote Down | Down | 200 OK, VotesDown +1 |
| Up | Vote Up | Up | 200 OK, aucun changement |
| Up | Vote Down | Down | 200 OK, Up -1, Down +1 |
| Down | Vote Up | Up | 200 OK, Up +1, Down -1 |
| Down | Vote Down | Down | 200 OK, aucun changement |

### Comportement Attendu

#### Sur la Page Liste des Propositions (`/propositions`)
**Option 1 (Simple)** :
- Pas d'affichage des votes
- Navigation vers détails pour voter

**Option 2 (Complète)** :
- Affichage des compteurs sur chaque carte
- Boutons de vote inline (optionnel)

**Recommandation** : Option 1 pour commencer, Option 2 si temps disponible

#### Sur la Page Détails d'une Proposition (`/proposition/[id]`)

**Section Votes** (à ajouter) :
```
┌─────────────────────────────────────┐
│  [▲ Upvote]    42 votes positifs    │
│  [▼ Downvote]   5 votes négatifs    │
│                                     │
│  Score : +37                        │
└─────────────────────────────────────┘
```

**États visuels des boutons** :
- Non authentifié : Boutons grisés + tooltip "Connectez-vous pour voter"
- Authentifié + pas voté : Boutons actifs, style neutre
- Authentifié + voté Up : Bouton Up actif (vert), Down neutre
- Authentifié + voté Down : Bouton Down actif (rouge), Up neutre
- Chargement : Boutons désactivés + spinner

**Actions** :
1. Click sur un bouton → Appel API
2. Pendant l'appel : Désactiver les boutons + spinner
3. Succès :
   - Mise à jour des compteurs
   - Mise à jour du style des boutons
   - Toast de confirmation
4. Erreur :
   - Toast d'erreur avec message
   - Restauration de l'état précédent

### Cas Limites

- **Utilisateur non authentifié** : Boutons désactivés, message pour se connecter
- **Proposition supprimée** : 404 Not Found (déjà géré backend)
- **Rate limit atteint** : Toast d'erreur "Trop de votes, réessayez plus tard"
- **Erreur réseau** : Toast d'erreur générique + retry manuel
- **Vote simultané** (deux onglets) : Dernier vote gagne, compteurs se synchronisent
- **Proposition sans votes** : Afficher "0" pour VotesUp et VotesDown

---

## 🏗️ Conception Technique

### Architecture

#### Couches Impactées

- [ ] **Web (Backend)** : Aucune modification nécessaire ✅
- [ ] **Business** : Aucune modification nécessaire ✅
- [ ] **Dal** : Aucune modification nécessaire ✅
- [x] **Frontend Pages** : Modification de `proposition/[id].vue`
- [x] **Frontend Composables** : Déjà existant (`usePropositionApi`)
- [x] **Frontend Types** : Ajout optionnel de `VoteType` enum

#### Diagramme de Flux

```
┌─────────────────┐
│  Utilisateur    │
└────────┬────────┘
         │
         │ 1. Navigue vers /proposition/123
         ▼
┌──────────────────────────────────────┐
│  Page: proposition/[id].vue          │
│  - Charge la proposition (GET)       │
│  - Affiche titre, description        │
│  - Affiche VotesUp, VotesDown        │
│  - Affiche boutons de vote           │
└────────┬─────────────────────────────┘
         │
         │ 2. Click sur "Vote Up"
         ▼
┌──────────────────────────────────────┐
│  Composable: usePropositionApi       │
│  - Appel voteUp(propositionId)       │
└────────┬─────────────────────────────┘
         │
         │ 3. POST /api/propositions/123/vote-up
         ▼
┌──────────────────────────────────────┐
│  Backend: PropositionsController     │
│  - Validate user                     │
│  - PropositionService.VoteUpAsync()  │
└────────┬─────────────────────────────┘
         │
         │ 4. Retourne PropositionDto mis à jour
         ▼
┌──────────────────────────────────────┐
│  Frontend: Mise à jour réactive      │
│  - proposition.value = response      │
│  - Toast de succès                   │
│  - Boutons mis à jour visuellement   │
└──────────────────────────────────────┘
```

### Interfaces Publiques

#### API Endpoints (Backend - Déjà Existants ✅)

**Vote Up**
```
POST /api/propositions/{id}/vote-up
Authorization: Bearer {token}
Rate Limit: 30/minute

Response 200 OK:
{
  "id": 123,
  "title": "Organiser une soirée jeux de société",
  "description": "...",
  "votesUp": 43,    // Incrémenté
  "votesDown": 5,
  "userId": 1,
  "user": {...},
  "createdAt": "2026-01-10T10:00:00Z"
}

Response 404: Proposition not found
Response 401: Unauthorized
Response 429: Too Many Requests
```

**Vote Down**
```
POST /api/propositions/{id}/vote-down
Authorization: Bearer {token}
Rate Limit: 30/minute

Response: Identique à vote-up
```

#### Frontend Composables (Déjà Existants ✅)

**Fichier** : `app/composables/usePropositionApi.ts`

```typescript
const { voteUp, voteDown } = usePropositionApi()

// Déjà implémentées :
const voteUp = async (id: number): Promise<PropositionDto> => {
    return await api.post(`/Propositions/${id}/vote-up`, {}) as PropositionDto
}

const voteDown = async (id: number): Promise<PropositionDto> => {
    return await api.post(`/Propositions/${id}/vote-down`, {}) as PropositionDto
}
```

#### Types Frontend (À Compléter)

**Fichier** : `app/types/proposition.ts`

**Existant** :
```typescript
export interface PropositionDto {
    id: number
    title: string
    description: string
    createdAt: string
    userId: number
    user: UserDto
    votesUp: number      // ✅ Déjà présent
    votesDown: number    // ✅ Déjà présent
}
```

**À ajouter (optionnel)** :
```typescript
export enum VoteType {
    Up = 1,
    Down = -1,
    None = 0
}

export interface PropositionVoteState {
    currentVote: VoteType
    isLoading: boolean
}
```

### Modèles de Données

#### DTOs (Backend - Déjà Complet ✅)

Pas de modification nécessaire. Les DTOs existants contiennent déjà `votesUp` et `votesDown`.

#### État Frontend (À Implémenter)

**Dans le composant `proposition/[id].vue`** :

```typescript
const proposition = ref<PropositionDto | null>(null)
const currentUserVote = ref<VoteType>(VoteType.None)  // À tracker
const isVoting = ref(false)  // État de chargement
```

### Flux de Données

#### Chargement Initial de la Page

```typescript
onMounted(async () => {
    await loadProposition()
})

const loadProposition = async () => {
    try {
        isLoading.value = true
        proposition.value = await getPropositionById(propositionId)

        // TODO: Déterminer le vote personnel de l'utilisateur
        // Option 1: Comparer les compteurs avant/après
        // Option 2: Créer un endpoint GET /api/propositions/{id}/my-vote
        currentUserVote.value = await detectUserVote()
    } catch (error) {
        toast.add({ title: 'Erreur', description: 'Impossible de charger la proposition', color: 'red' })
    } finally {
        isLoading.value = false
    }
}
```

#### Action de Vote

```typescript
const handleVoteUp = async () => {
    if (!isAuthenticated.value) {
        toast.add({ title: 'Connexion requise', description: 'Connectez-vous pour voter', color: 'orange' })
        return
    }

    try {
        isVoting.value = true
        const previousVote = currentUserVote.value

        // Optimistic update
        currentUserVote.value = VoteType.Up
        updateVoteCountersOptimistically(previousVote, VoteType.Up)

        // API call
        const updatedProposition = await voteUp(propositionId)
        proposition.value = updatedProposition

        toast.add({ title: 'Vote enregistré', description: 'Votre vote a été pris en compte', color: 'green' })
    } catch (error) {
        // Rollback optimistic update
        toast.add({ title: 'Erreur', description: 'Impossible d\'enregistrer le vote', color: 'red' })
        await loadProposition()  // Recharger les vraies données
    } finally {
        isVoting.value = false
    }
}

const handleVoteDown = async () => {
    // Implémentation similaire
}
```

### Dépendances

- **Packages Backend** : Aucun (déjà installé)
- **Packages Frontend** : Aucun nouveau
  - Utilise Nuxt UI pour les boutons et toasts
  - Utilise les composables existants
- **Services externes** : Aucun
- **Migrations DB** : Aucune (structure déjà en place)

---

## 🔒 Sécurité

### Authentification & Autorisation

- **Lecture des votes** : Public (tous)
- **Voter** : Authentifié uniquement
- **Claims nécessaires** : Token JWT valide
- **Rate limiting** : ✅ Déjà implémenté backend (30 votes/minute/IP)

### Validation des Données

**Backend** : ✅ Déjà implémenté
- Validation de l'utilisateur (email exists)
- Validation de la proposition (exists + not deleted)
- Contrainte unique (UserId, PropositionId) en base

**Frontend** :
- Vérification de l'authentification avant d'afficher les boutons actifs
- Désactivation des boutons pendant le chargement
- Gestion des erreurs réseau

### Protection Contre les Vulnérabilités

- [x] **Rate limiting** : ✅ Backend (30/min/IP)
- [x] **CSRF** : ✅ Tokens JWT
- [x] **SQL Injection** : ✅ EF Core paramétré
- [x] **Vote multiple** : ✅ Contrainte UNIQUE en BD
- [x] **XSS** : ✅ Vue/Nuxt encode automatiquement

### Audit et Logging

**Backend** : ✅ Logging existant
- Log des votes dans `PropositionService`
- Timestamps en base (CreatedAt, UpdatedAt)

**Frontend** :
- Console.error pour les erreurs de vote
- Toast notifications pour le feedback utilisateur

---

## 🧪 Stratégie de Tests

### Tests Unitaires

#### Backend (À Ajouter ❌)

**Tests manquants critiques** :

**Fichier** : `Tests/Services/PropositionServiceTests.cs`

```csharp
[TestMethod]
public async Task VoteUpAsync_NewVote_AddsVoteAndIncrementsCounter()
{
    // Arrange
    var propositionId = 1;
    var userEmail = "test@example.com";

    var user = new UserBo { Id = 1, Email = userEmail };
    var proposition = new PropositionBo
    {
        Id = propositionId,
        VotesUp = 5,
        VotesDown = 2,
        IsDeleted = false
    };

    _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
    _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
    _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
        .ReturnsAsync((PropositionVoteBo?)null);
    _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(6);

    // Act
    var result = await _propositionService.VoteUpAsync(propositionId, userEmail);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(6, result.VotesUp);
    _mockPropositionVoteRepository.Verify(r => r.AddAsync(It.IsAny<PropositionVoteBo>()), Times.Once);
}

[TestMethod]
public async Task VoteUpAsync_ChangeFromDown_UpdatesVoteAndCounters()
{
    // Test pour changer de Down à Up
}

[TestMethod]
public async Task VoteUpAsync_AlreadyUp_NoChange()
{
    // Test d'idempotence
}
```

**Tests Controller** :

**Fichier** : `Tests/Controllers/PropositionsControllerTests.cs`

```csharp
[TestMethod]
public async Task VoteUp_ValidRequest_ReturnsOkWithUpdatedProposition()
{
    // Arrange
    var propositionId = 1;
    var updatedProposition = new PropositionDto
    {
        Id = propositionId,
        VotesUp = 6,
        VotesDown = 2
    };

    _mockPropositionService.Setup(s => s.VoteUpAsync(propositionId, TestUserEmail))
        .ReturnsAsync(updatedProposition);

    // Act
    var result = await _controller.VoteUp(propositionId);

    // Assert
    Assert.IsInstanceOfType<OkObjectResult>(result.Result);
    var okResult = (OkObjectResult)result.Result;
    var dto = okResult.Value as PropositionDto;
    Assert.AreEqual(6, dto.VotesUp);
}

[TestMethod]
public async Task VoteUp_PropositionNotFound_ReturnsNotFound()
{
    // Test 404
}

[TestMethod]
public async Task VoteUp_Unauthorized_Returns401()
{
    // Test non authentifié
}
```

#### Frontend (E2E Playwright - Optionnel)

**Fichier** : `tests/e2e/proposition-voting.spec.ts`

```typescript
test('User can vote up on a proposition', async ({ page }) => {
    // Login
    await page.goto('/login')
    await page.fill('input[name="email"]', 'test@example.com')
    await page.fill('input[name="password"]', 'password')
    await page.click('button[type="submit"]')

    // Navigate to proposition
    await page.goto('/proposition/1')

    // Vote up
    await page.click('button:has-text("Upvote")')

    // Verify
    await expect(page.locator('.toast')).toContainText('Vote enregistré')
    await expect(page.locator('.votes-up-count')).toContainText('43')
})
```

### Scénarios à Tester

#### Cas Nominaux (Happy Path)
- [ ] Affichage des compteurs de votes
- [ ] Vote Up avec succès (nouveau vote)
- [ ] Vote Down avec succès (nouveau vote)
- [ ] Changement de vote (Up → Down)
- [ ] Changement de vote (Down → Up)
- [ ] Vote idempotent (Up → Up, aucun changement)
- [ ] Feedback toast après vote

#### Cas d'Erreur
- [ ] Vote sans authentification → Toast d'erreur
- [ ] Vote sur proposition inexistante → 404
- [ ] Erreur réseau → Toast d'erreur + restauration état
- [ ] Rate limit atteint → Toast d'erreur
- [ ] Token JWT expiré → Redirection login

#### Cas Limites
- [ ] Proposition avec 0 votes
- [ ] Proposition avec beaucoup de votes (>1000)
- [ ] Vote simultané dans deux onglets
- [ ] Vote pendant chargement (boutons désactivés)

### Couverture Cible

**Backend** :
- Service : 90%+ (actuellement ~60%, manque tests de vote)
- Controller : 80%+ (actuellement ~70%, manque tests de vote)
- Repository : 80%+ (déjà atteint)

**Frontend** :
- Tests E2E pour les scénarios critiques (optionnel)

---

## 📦 Plan d'Implémentation

### Étapes d'Implémentation

#### Phase 1 : Backend - Tests (Critique) ⏱️ 1-2h

1. [ ] **Créer tests unitaires pour VoteUpAsync** (Service)
   - Nouveau vote
   - Changement de vote
   - Vote idempotent
   - Erreurs (user not found, proposition not found)

2. [ ] **Créer tests unitaires pour VoteDownAsync** (Service)
   - Mêmes scénarios que VoteUp

3. [ ] **Créer tests Controller pour VoteUp et VoteDown**
   - 200 OK avec succès
   - 404 Not Found
   - 401 Unauthorized

4. [ ] **Exécuter tests** : `pwsh -File run-coverage.ps1`
   - Vérifier couverture ≥ 80%
   - Corriger les tests échoués

#### Phase 2 : Frontend - Détection du Vote Personnel ⏱️ 2-3h

**Problème** : Le backend ne retourne pas le vote personnel de l'utilisateur dans `PropositionDto`.

**Solution 1 (Simple - Recommandée)** : Tracking côté client

```typescript
// État local pour tracker le vote de l'utilisateur
const userVoteState = ref<VoteType>(VoteType.None)

// Méthode pour détecter le vote après action
const updateUserVoteFromResponse = (oldProp: PropositionDto, newProp: PropositionDto) => {
    // Si VotesUp a augmenté et VotesDown n'a pas changé → user a voté Up
    if (newProp.votesUp > oldProp.votesUp && newProp.votesDown === oldProp.votesDown) {
        userVoteState.value = VoteType.Up
    }
    // Si VotesDown a augmenté et VotesUp n'a pas changé → user a voté Down
    else if (newProp.votesDown > oldProp.votesDown && newProp.votesUp === oldProp.votesUp) {
        userVoteState.value = VoteType.Down
    }
    // Si VotesUp a augmenté et VotesDown a diminué → user a changé Down → Up
    else if (newProp.votesUp > oldProp.votesUp && newProp.votesDown < oldProp.votesDown) {
        userVoteState.value = VoteType.Up
    }
    // Si VotesDown a augmenté et VotesUp a diminué → user a changé Up → Down
    else if (newProp.votesDown > oldProp.votesDown && newProp.votesUp < oldProp.votesUp) {
        userVoteState.value = VoteType.Down
    }
}
```

**Limitations** :
- ⚠️ Pas persistant entre les rechargements de page
- ⚠️ Si un autre utilisateur vote en même temps, détection incorrecte

**Solution 2 (Robuste - Future)** : Créer endpoint backend

```csharp
// Backend - À ajouter si nécessaire plus tard
[HttpGet("{id}/my-vote")]
public async Task<ActionResult<VoteType?>> GetMyVote(int id)
{
    var email = User.GetUserEmailOrThrow();
    var vote = await propositionService.GetUserVoteAsync(id, email);
    return Ok(vote);
}
```

**Recommandation** : Commencer avec Solution 1, passer à Solution 2 si problème.

5. [ ] **Implémenter tracking du vote personnel** (Solution 1)
   - Ajouter état `userVoteState`
   - Méthode `updateUserVoteFromResponse`
   - Initialiser à `None` au chargement

#### Phase 3 : Frontend - UI de Vote ⏱️ 3-4h

6. [ ] **Modifier `app/pages/proposition/[id].vue`**

   **Ajouter section Votes** (après le titre, avant la description) :

   ```vue
   <template>
       <!-- ... titre et auteur existants ... -->

       <!-- Section Votes -->
       <div class="my-6 p-4 rounded-lg bg-surface-light dark:bg-surface-dark border border-accented">
           <h3 class="text-lg font-semibold mb-4">Votes</h3>

           <div class="flex items-center gap-4">
               <!-- Bouton Vote Up -->
               <UButton
                   :icon="userVoteState === VoteType.Up ? 'i-heroicons-arrow-up-solid' : 'i-heroicons-arrow-up'"
                   :color="userVoteState === VoteType.Up ? 'green' : 'gray'"
                   :disabled="!isAuthenticated || isVoting"
                   :loading="isVoting"
                   @click="handleVoteUp"
               >
                   <template #trailing>
                       <span class="font-semibold">{{ proposition?.votesUp || 0 }}</span>
                   </template>
               </UButton>

               <!-- Bouton Vote Down -->
               <UButton
                   :icon="userVoteState === VoteType.Down ? 'i-heroicons-arrow-down-solid' : 'i-heroicons-arrow-down'"
                   :color="userVoteState === VoteType.Down ? 'red' : 'gray'"
                   :disabled="!isAuthenticated || isVoting"
                   :loading="isVoting"
                   @click="handleVoteDown"
               >
                   <template #trailing>
                       <span class="font-semibold">{{ proposition?.votesDown || 0 }}</span>
                   </template>
               </UButton>

               <!-- Score Net (Optionnel) -->
               <div class="ml-auto">
                   <span class="text-sm text-muted-light dark:text-muted-dark">Score :</span>
                   <span class="text-lg font-bold ml-2" :class="voteScoreClass">
                       {{ voteScore > 0 ? '+' : '' }}{{ voteScore }}
                   </span>
               </div>
           </div>

           <!-- Message pour non-authentifiés -->
           <p v-if="!isAuthenticated" class="text-sm text-muted-light dark:text-muted-dark mt-2">
               Connectez-vous pour voter sur cette proposition
           </p>
       </div>

       <!-- ... description existante ... -->
   </template>
   ```

   **Ajouter logique dans le script** :

   ```vue
   <script setup lang="ts">
   import type { PropositionDto } from '~/types/proposition'

   // États
   const proposition = ref<PropositionDto | null>(null)
   const userVoteState = ref<number>(0) // 0: None, 1: Up, -1: Down
   const isVoting = ref(false)
   const isLoading = ref(true)

   // Composables
   const { getPropositionById, voteUp, voteDown } = usePropositionApi()
   const { isAuthenticated } = useAuth()
   const toast = useToast()
   const route = useRoute()

   // Computed
   const propositionId = computed(() => parseInt(route.params.id as string))

   const voteScore = computed(() => {
       if (!proposition.value) return 0
       return proposition.value.votesUp - proposition.value.votesDown
   })

   const voteScoreClass = computed(() => {
       if (voteScore.value > 0) return 'text-green-600 dark:text-green-400'
       if (voteScore.value < 0) return 'text-red-600 dark:text-red-400'
       return 'text-gray-600 dark:text-gray-400'
   })

   // Méthodes
   const loadProposition = async () => {
       try {
           isLoading.value = true
           proposition.value = await getPropositionById(propositionId.value)
       } catch (error) {
           console.error('Error loading proposition:', error)
           toast.add({
               title: 'Erreur',
               description: 'Impossible de charger la proposition',
               color: 'red'
           })
       } finally {
           isLoading.value = false
       }
   }

   const handleVoteUp = async () => {
       if (!proposition.value) return

       try {
           isVoting.value = true
           const previousVotesUp = proposition.value.votesUp
           const previousVotesDown = proposition.value.votesDown

           const updatedProposition = await voteUp(propositionId.value)

           // Détection du changement pour mettre à jour userVoteState
           if (updatedProposition.votesUp > previousVotesUp &&
               updatedProposition.votesDown === previousVotesDown) {
               userVoteState.value = 1 // Nouveau vote Up
           } else if (updatedProposition.votesUp > previousVotesUp &&
                      updatedProposition.votesDown < previousVotesDown) {
               userVoteState.value = 1 // Changé de Down à Up
           }
           // Sinon, aucun changement (déjà voté Up)

           proposition.value = updatedProposition

           toast.add({
               title: 'Vote enregistré',
               description: 'Votre vote positif a été pris en compte',
               color: 'green'
           })
       } catch (error: any) {
           console.error('Error voting:', error)

           let errorMessage = 'Impossible d\'enregistrer le vote'
           if (error.response?.status === 429) {
               errorMessage = 'Trop de votes, réessayez plus tard'
           }

           toast.add({
               title: 'Erreur',
               description: errorMessage,
               color: 'red'
           })
       } finally {
           isVoting.value = false
       }
   }

   const handleVoteDown = async () => {
       // Implémentation similaire à handleVoteUp
   }

   // Lifecycle
   onMounted(async () => {
       await loadProposition()
   })
   </script>
   ```

7. [ ] **Tester manuellement**
   - Charger la page → Voir les compteurs
   - Voter Up → Compteur Up +1, toast de succès
   - Voter Down → Compteur Down +1, Up -1, toast
   - Recharger la page → État perdu (normal avec Solution 1)
   - Tester sans authentification → Boutons désactivés

#### Phase 4 : Frontend - Liste des Propositions (Optionnel) ⏱️ 1h

8. [ ] **Modifier `app/pages/propositions.vue`** (Optionnel)

   **Option Simple** : Ajouter juste les compteurs en lecture seule

   ```vue
   <div class="flex items-center gap-2 text-sm text-muted-light dark:text-muted-dark">
       <span>▲ {{ proposition.votesUp }}</span>
       <span>▼ {{ proposition.votesDown }}</span>
   </div>
   ```

   **Option Complète** : Boutons de vote inline (plus complexe, à décider)

9. [ ] **Modifier `app/components/propositions/items.vue`**
   - Ajouter affichage des votes sur chaque carte

#### Phase 5 : Tests E2E (Optionnel) ⏱️ 2-3h

10. [ ] **Créer `tests/e2e/proposition-voting.spec.ts`**
    - Test vote up
    - Test vote down
    - Test changement de vote
    - Test non authentifié

#### Phase 6 : Documentation et Finalisation ⏱️ 30min

11. [ ] **Mettre à jour ce document**
    - Statut → ✅ Implémenté
    - Ajouter section "Modifications Post-Implémentation"
    - Documenter les choix techniques

12. [ ] **Mettre à jour README** (si nécessaire)
    - Ajouter section sur le système de vote

### Fichiers à Créer/Modifier

#### Fichiers à Créer

- [ ] `Tests/Services/PropositionServiceVotingTests.cs` - Tests de vote service
- [ ] `tests/e2e/proposition-voting.spec.ts` - Tests E2E (optionnel)

#### Fichiers à Modifier

**Backend** :
- [x] `Tests/Services/PropositionServiceTests.cs` - Ajouter tests de vote
- [x] `Tests/Controllers/PropositionsControllerTests.cs` - Ajouter tests VoteUp/VoteDown

**Frontend** :
- [x] `app/pages/proposition/[id].vue` - Ajouter UI de vote
- [ ] `app/pages/propositions.vue` - Afficher votes (optionnel)
- [ ] `app/components/propositions/items.vue` - Afficher votes (optionnel)
- [ ] `app/types/proposition.ts` - Ajouter VoteType enum (optionnel)

### Ordre de Dépendance

```
1. Tests Backend (Service + Controller)
   ↓
2. Tracking vote personnel (logique)
   ↓
3. UI de vote (page détails)
   ↓
4. Tests manuels
   ↓
5. (Optionnel) Votes sur liste
   ↓
6. (Optionnel) Tests E2E
   ↓
7. Documentation
```

**Pas de blocage technique** : Toutes les étapes peuvent être faites séquentiellement.

### Estimation de Temps

| Phase | Durée Estimée | Critique |
|-------|---------------|----------|
| Tests Backend | 1-2h | ✅ Oui |
| Tracking vote | 2-3h | ✅ Oui |
| UI vote détails | 3-4h | ✅ Oui |
| Tests manuels | 1h | ✅ Oui |
| Votes sur liste | 1h | ⚠️ Optionnel |
| Tests E2E | 2-3h | ⚠️ Optionnel |
| Documentation | 30min | ✅ Oui |
| **TOTAL** | **8-14h** | - |

**Total critique** : 7.5-10.5h
**Total optionnel** : 3-3.5h

---

## 🚀 Déploiement

### Prérequis

- ✅ Backend ESN-WebApi en cours d'exécution
- ✅ Base de données avec tables Propositions et PropositionVotes
- ✅ Frontend Nuxt en développement
- ✅ Compte utilisateur authentifié pour tester

### Migrations de Base de Données

**Aucune migration nécessaire** ✅ (structure déjà en place depuis décembre 2024)

### Configuration

**Aucune configuration supplémentaire nécessaire** ✅

### Ordre de Déploiement

1. Backend : Aucun changement (déjà déployé)
2. Frontend : Déployer les modifications UI
3. Tests : Exécuter la suite de tests
4. Validation : Tester en production

---

## 📚 Documentation à Mettre à Jour

- [x] Ce document de spec
- [ ] README.md - Ajouter section "Vote sur propositions"
- [ ] Screenshots (optionnel) - Capturer l'interface de vote
- [ ] Guide utilisateur (optionnel)

---

## ✅ Checklist de Validation

### Avant Implémentation

- [x] Backend analysé et compris ✅
- [x] Endpoints API documentés ✅
- [x] Architecture respecte séparation en couches ✅
- [x] Sécurité prise en compte (auth, rate limiting) ✅
- [x] Stratégie de tests définie ✅
- [ ] Utilisateur a validé l'approche ⏳

### Après Implémentation

- [ ] Tests backend passent (coverage ≥ 80%)
- [ ] UI de vote fonctionnelle
- [ ] Boutons réactifs et désactivés pendant chargement
- [ ] Toast notifications appropriées
- [ ] Vote personnel visible
- [ ] Changement de vote fonctionnel
- [ ] Utilisateurs non authentifiés ne peuvent pas voter
- [ ] Rate limiting respecté
- [ ] Pas de warnings du compilateur
- [ ] Tests manuels OK
- [ ] Documentation mise à jour

---

## 🎯 Critères d'Acceptation

### Fonctionnels

- [ ] Les compteurs VotesUp et VotesDown sont affichés sur la page de détails
- [ ] Les boutons de vote sont visibles et fonctionnels pour les utilisateurs authentifiés
- [ ] Le vote personnel de l'utilisateur est visuellement distinct
- [ ] Un utilisateur peut changer son vote (Up ↔ Down)
- [ ] Toast de confirmation après chaque vote
- [ ] Toast d'erreur si vote échoue
- [ ] Boutons désactivés pour les utilisateurs non authentifiés
- [ ] Les compteurs se mettent à jour après chaque vote

### Techniques

- [ ] Code respecte conventions Vue 3 Composition API
- [ ] Composables existants réutilisés
- [ ] Pas de duplication de code
- [ ] Gestion d'erreurs complète (try-catch, toast)
- [ ] Loading states affichés
- [ ] Tests backend passent (≥ 80% coverage)
- [ ] Pas de warnings TypeScript/ESLint

### Non-Fonctionnels

- [ ] Performance : Vote enregistré en < 500ms
- [ ] Sécurité : Rate limiting respecté
- [ ] UX : Feedback immédiat après action
- [ ] Logs : Erreurs loggées en console
- [ ] Accessibilité : Boutons avec labels appropriés

---

## 📝 Notes et Décisions

### Décisions de Conception

#### 1. Tracking du Vote Personnel

**Décision** : Commencer avec tracking côté client (Solution 1)

**Pourquoi** :
- Pas besoin de modifier le backend
- Plus rapide à implémenter
- Suffisant pour la v1

**Alternatives considérées** :
- Créer endpoint `GET /api/propositions/{id}/my-vote` (plus robuste, mais overkill pour v1)

**Trade-off** : État perdu au rechargement de page (acceptable pour v1)

#### 2. Affichage des Votes

**Décision** : Priorité sur la page de détails, optionnel sur la liste

**Pourquoi** :
- Page de détails = contexte principal du vote
- Liste déjà chargée, ajout complexifie l'UI

**Future** : Ajouter votes inline sur la liste si demande utilisateur

#### 3. Retrait de Vote

**Décision** : Ne pas implémenter pour v1

**Pourquoi** :
- Backend ne semble pas supporter (voter deux fois même option = idempotent, pas de suppression)
- Ajouterait de la complexité UI
- Changer de vote suffit pour l'instant

**Future** : À implémenter si demande utilisateur forte

### Alternatives Considérées

#### Alternative 1 : Modal de Confirmation

**Rejetée** : Trop de friction, vote doit être rapide

#### Alternative 2 : Système de "Like" Simple

**Rejetée** : Backend implémente déjà Up/Down, cohérent avec Reddit/StackOverflow

### Points d'Attention

- **Concurrence** : Si deux utilisateurs votent en même temps, les compteurs peuvent être légèrement désynchronisés temporairement. Résolu au prochain rechargement.
- **Détection du vote personnel** : Solution 1 peut échouer si autre utilisateur vote simultanément. Acceptable pour v1.
- **Performance** : Compteurs dénormalisés garantissent des lectures rapides (pas de COUNT(*) à chaque affichage)

### Questions Ouvertes

- **Affichage public des votes** : Faut-il permettre aux admins de voir qui a voté quoi ? (données existent en BD)
- **Analytics** : Faut-il tracker les statistiques de vote (graphiques temporels, tendances) ?
- **Notifications** : Faut-il notifier l'auteur d'une proposition quand quelqu'un vote ?

---

## 📊 Suivi

| Date | Statut | Commentaire |
|------|--------|-------------|
| 2026-01-12 | 🟡 En Documentation | Création du document de spec après analyse complète du backend |
| | 🔵 Validé | ⏳ En attente validation utilisateur |
| | 🟢 Implémenté | |
| | ✅ Testé | |

---

**Ce document suit le template standard du projet ESN-WebApi**
**Backend déjà complet ✅ - Feature principalement frontend**
**Estimation totale : 8-14h (7.5-10.5h critique + 3-3.5h optionnel)**
