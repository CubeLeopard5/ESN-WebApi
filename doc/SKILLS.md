# 🛠️ Guide des Skills Claude - ESN-WebApi

> **Skills** : Extensions qui permettent à Claude de suivre des workflows spécialisés pour ce projet

**Dernière mise à jour** : 2026-01-10

---

## 📋 Skills Disponibles (5)

### 1. 📋 /code-review - Code Review Complet

**Déclenché par** : "fais une code review", "review mon code"

**Fonction** : Analyse complète du code selon 5 axes critiques

**Axes analysés**
1. ✅ **Qualité du code** - SOLID, Clean Code, conventions C#
2. 🧪 **Tests** - Couverture (min 80%), qualité, cas limites
3. 🔒 **Sécurité** - OWASP Top 10, vulnérabilités
4. 🏗️ **Architecture** - Respect des couches, patterns
5. 📚 **Documentation** - Commentaires XML, docs API

**Quand l'utiliser**
- Avant de créer une pull request
- Après avoir implémenté une feature
- Audit périodique du code

**Exemple**
```
Vous : Claude, fais une code review de mes changements

Claude : [Analyse le code...]

📋 Rapport de Code Review

✅ Points Positifs
- Architecture bien respectée
- Tests complets (89%)

⚠️ Suggestions
- EventsController.cs:45 : Ajouter validation

🎯 Recommandation : ✅ APPROUVÉ
Score : 8.5/10
```

---

### 2. 📝 /doc-first - Documentation-First Workflow

**Déclenché par** : Automatique sur toute demande d'implémentation

**Principe** : **JAMAIS de code sans documentation préalable**

**Processus**
1. 📝 **Documentation** → Créer spec dans `doc/specs/YYYYMMDD-nom.md`
2. ✅ **Validation** → Obtenir votre approbation
3. 💻 **Implémentation** → Coder selon la doc
4. 🧪 **Tests** → Vérifier couverture ≥ 80%

**Exceptions** (seuls cas où on peut skip)
- Typos (fautes d'orthographe)
- Formatting (indentation)
- Commentaires simples
- Logs de debug temporaires

**Exemple**
```
Vous : Claude, ajoute un endpoint pour créer des utilisateurs

Claude : Je vais d'abord créer un document de conception.
         [Crée doc/specs/20260110-add-user-creation.md]
         [Présente la conception]

         Valider pour implémenter ?

Vous : Oui

Claude : [Implémente selon la doc]
         [Crée les tests]
         [Exécute run-coverage.ps1]

         ✅ Feature terminée ! Coverage : 92%
```

---

### 3. ⚡ /crud-generator - Générateur CRUD Complet

**Déclenché par** : `/crud-generator EntityName`

**Fonction** : Génère toute la stack CRUD en quelques minutes

**Génère automatiquement**
- 📦 `Bo/{Entity}.cs` - Entité du domaine
- 📋 `Dto/{Entity}Dto.cs` - DTOs (Create, Update, Response)
- 💾 `Dal/Repositories/I{Entity}Repository.cs` + implémentation
- 🎯 `Business/Services/I{Entity}Service.cs` + implémentation
- 🌐 `Web/Controllers/{Entity}sController.cs`
- 🧪 `Tests/Business/{Entity}ServiceTests.cs`
- 🔧 Mise à jour DbContext + DI

**Gain de temps** : 30-45 minutes par entité

**Exemple**
```
Vous : /crud-generator Product

Claude : Questions :
         1. Propriétés ? (Name, Price, Stock...)
         2. Relations ? (Category N-1)
         3. Sécurité ? (Admin pour CUD, Public pour R)

         [Génère tous les fichiers...]

         ✅ Stack générée !

         Prochaines étapes :
         dotnet ef migrations add AddProductEntity
         dotnet ef database update
         pwsh -File run-coverage.ps1
```

---

### 4. 🚀 /performance-audit - Audit de Performance

**Déclenché par** : "audit de performance", "optimise les performances"

**Fonction** : Détecte et corrige les problèmes de performance

**Détecte**
- 🔴 **N+1 queries** - Requêtes multiples au lieu d'une
- 📊 **Index manquants** - Sur FK, WHERE, ORDER BY
- 💾 **AsNoTracking absent** - Tracking inutile sur lectures
- 📈 **Include excessifs** - Chargement de trop de relations
- 📄 **Absence de pagination** - Retour de milliers de lignes
- ⚡ **Opérations synchrones** - .Result, .Wait() au lieu de await

**Résultat** : Rapport détaillé avec solutions et gains estimés

**Exemple**
```
Vous : Claude, fais un audit de performance

Claude : [Analyse Repositories, Services, Controllers...]

         📊 23 fichiers analysés, 12 problèmes détectés

         🔴 3 critiques :
         1. N+1 query dans EventRepository
         2. Pas de pagination sur /api/events
         3. Index manquant sur Event.CalendarId

         🟡 5 importants :
         AsNoTracking manquant dans 5 repositories

         💰 Gain estimé : ⬆️ 70% performance

         [Génère rapport complet avec solutions...]
```

---

### 5. 📝 /commit-message - Messages de Commit Professionnels

**Déclenché par** : Automatique lors de création de commit

**Fonction** : Génère messages selon Conventional Commits

**Format**
```
type(scope): description courte

- Détail du changement 1
- Détail du changement 2

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Types**
- `feat` - Nouvelle fonctionnalité
- `fix` - Correction de bug
- `refactor` - Refactoring sans changement fonctionnel
- `perf` - Amélioration de performance
- `test` - Ajout/modification de tests
- `docs` - Documentation uniquement
- `chore` - Maintenance

**Scopes du projet**
- `events`, `calendars`, `users`, `api`, `dal`, `business`, `tests`, `config`, `security`

**Exemple**
```
Vous : Claude, créer un commit

Claude : [Analyse git status et git diff]

         Message proposé :

         feat(events): add pagination to event list endpoint

         - Add PaginationDto with PageNumber and PageSize
         - Update EventsController.GetAll
         - Add unit tests for pagination

         Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>

         Valider ?
```

---

## 🔧 Configuration

### Hooks Automatiques

**PostToolUse (Write/Edit)**
- Rappel d'exécuter les tests après modification de fichiers `.cs`

```
🧪 Code .NET modifié. Pensez à exécuter les tests avec: pwsh -File run-coverage.ps1
```

### Permissions Configurées

Dans `.claude/settings.local.json` :
- Exécution des tests avec couverture
- Build et test dotnet
- Migrations Entity Framework

---

## 📚 Workflows Recommandés

### Workflow 1 : Nouvelle Feature CRUD

```bash
# 1. Générer la stack
/crud-generator Product

# 2. Créer migration
dotnet ef migrations add AddProductEntity --project Dal --startup-project Web
dotnet ef database update --project Dal --startup-project Web

# 3. Tester
pwsh -File run-coverage.ps1

# 4. Commit
Claude, créer un commit
# → Message feat(products): add CRUD endpoints for Product
```

**Temps** : ~5 minutes au lieu de 45 minutes

---

### Workflow 2 : Optimisation de Performance

```bash
# 1. Audit
Claude, fais un audit de performance

# 2. Lire le rapport et corriger les problèmes

# 3. Re-tester
pwsh -File run-coverage.ps1

# 4. Commit
Claude, créer un commit
# → Message perf(dal): optimize queries and add indexes
```

---

### Workflow 3 : Développement avec Doc-First

```bash
# 1. Demander feature
Claude, ajoute la pagination aux événements

# 2. Doc-first s'active automatiquement
# → Crée doc/specs/20260110-add-event-pagination.md
# → Vous validez
# → Implémente
# → Teste

# 3. Commit automatique avec message structuré
# → feat(events): add pagination to event list endpoint
```

---

## 📊 Récapitulatif

| Skill | Trigger | Gain Principal |
|-------|---------|----------------|
| **code-review** | Manuel | ✅ Qualité constante |
| **doc-first** | Auto | 📚 Doc toujours à jour |
| **crud-generator** | Manuel | ⚡ 30+ min/entité |
| **performance-audit** | Manuel | 🚀 60-80% perf |
| **commit-message** | Auto | 📝 Historique propre |

---

## 🎯 Bonnes Pratiques

1. **Utilisez /crud-generator** pour toutes les nouvelles entités
2. **Lancez /performance-audit** après chaque sprint
3. **Laissez /commit-message** s'activer automatiquement
4. **Code-review et doc-first** s'activent déjà automatiquement

---

## 🛠️ Personnalisation

Les skills peuvent être modifiés en éditant les fichiers SKILL.md :

```bash
# Éditer un skill
nano .claude/skills/crud-generator/SKILL.md

# Désactiver temporairement un skill
mv .claude/skills/doc-first .claude/skills/doc-first.disabled
```

---

## 📖 Fichiers de Référence

- **Définitions** : `.claude/skills/*/SKILL.md`
- **Configuration** : `.claude/settings.local.json`
- **Template specs** : `doc/specs/TEMPLATE.md`

---

**Note** : Les skills évoluent avec le projet. N'hésitez pas à les adapter à vos besoins spécifiques.
