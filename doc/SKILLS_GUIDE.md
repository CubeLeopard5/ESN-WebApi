# Guide des Skills Claude Code

Ce projet utilise des Skills Claude Code pour améliorer la qualité du code et standardiser les pratiques de développement.

## Skills Disponibles

### 1. `/code-review` - Code Review Complet

**Objectif** : Effectuer une analyse approfondie du code selon 5 axes critiques.

**Utilisation** :
```
Claude, fais une code review de mes changements récents
```

ou

```
/code-review
```

**Ce que le skill analyse** :
- **Qualité du code** : SOLID, Clean Code, conventions C#, complexité
- **Tests** : Couverture, qualité, nomenclature, cas limites
- **Sécurité** : OWASP Top 10, vulnérabilités, validation des données
- **Architecture** : Respect des couches, patterns, performance
- **Documentation** : Commentaires XML, documentation API

**Format du rapport** :
Le skill génère un rapport structuré avec :
- Vue d'ensemble
- Points positifs
- Problèmes identifiés par catégorie
- Actions requises (bloquants, importants, optionnels)
- Score global et recommandation finale

**Quand l'utiliser** :
- Avant de créer une pull request
- Après avoir implémenté une nouvelle feature
- Pour vérifier la qualité d'un module existant
- Périodiquement pour auditer le code

### 2. `/doc-first` - Workflow Documentation-First

**Objectif** : Forcer la documentation AVANT l'implémentation pour garantir clarté et maintenabilité.

**Principe** : JAMAIS de code sans documentation préalable.

**Processus** :
1. **Documentation** - Définir ce qu'on va faire
2. **Validation** - Obtenir l'approbation utilisateur
3. **Implémentation** - Coder selon la doc
4. **Tests** - Vérifier que tout fonctionne

**Utilisation** :

Le skill s'active automatiquement quand vous demandez une implémentation :

```
Claude, ajoute un endpoint pour créer des utilisateurs
```

Claude va alors :
1. Créer un document dans `doc/specs/` avec la conception complète
2. Vous le présenter pour validation
3. Attendre votre approbation
4. Implémenter selon la doc
5. Exécuter les tests

**Format du document de spec** :

Chaque implémentation génère un fichier dans `doc/specs/` avec :
- Contexte et objectif
- Spécifications fonctionnelles
- Conception technique détaillée
- Stratégie de tests
- Plan d'implémentation
- Critères d'acceptation

**Règles** :
- ❌ Jamais de code sans doc préalable
- ❌ Jamais d'implémentation sans validation utilisateur
- ✅ Toujours documenter les interfaces publiques
- ✅ Toujours exécuter les tests après implémentation

**Exceptions** (seuls cas où on peut skip la doc) :
- Typos (fautes d'orthographe)
- Formatting (indentation)
- Commentaires (ajout/correction)
- Logs de debug temporaires

Pour TOUT LE RESTE, le processus complet s'applique.

## Hooks Automatiques

### Rappel de Tests

Quand vous modifiez des fichiers `.cs` ou `.csproj`, un hook vous rappelle automatiquement d'exécuter les tests :

```
🧪 Code .NET modifié. Pensez à exécuter les tests avec: pwsh -File run-coverage.ps1
```

**Comment exécuter les tests** :
```powershell
pwsh -File run-coverage.ps1
```

Ce script :
1. Exécute tous les tests
2. Génère un rapport de couverture
3. Crée un rapport HTML dans `coverage-report/`
4. Ouvre automatiquement le rapport dans votre navigateur

## Workflow Recommandé

### Pour une Nouvelle Feature

1. **Demander l'implémentation**
   ```
   Claude, ajoute la fonctionnalité X
   ```

2. **Review de la doc** (automatique via `/doc-first`)
   - Claude crée le document de spec
   - Vous le reviewez
   - Vous validez ou demandez des ajustements

3. **Implémentation** (automatique)
   - Claude implémente selon la doc validée
   - Le code suit exactement le plan documenté

4. **Tests** (automatique)
   - Claude exécute les tests
   - Vérifie la couverture (minimum 80%)
   - Corrige si nécessaire

5. **Code Review** (optionnel mais recommandé)
   ```
   Claude, fais une code review de cette feature
   ```

### Pour un Bugfix

1. **Décrire le bug**
   ```
   Claude, il y a un bug dans X qui fait Y au lieu de Z
   ```

2. **Documentation** (via `/doc-first`)
   - Analyse du bug
   - Solution proposée
   - Tests à ajouter

3. **Fix + Tests**
   - Correction du bug
   - Ajout de tests de non-régression

4. **Vérification**
   ```powershell
   pwsh -File run-coverage.ps1
   ```

### Pour un Refactoring

1. **Demander le refactoring**
   ```
   Claude, refactor le module X pour améliorer Y
   ```

2. **Documentation**
   - État actuel
   - Améliorations proposées
   - Plan de refactoring

3. **Implémentation progressive**
   - Refactoring par petites étapes
   - Tests maintenus à jour

4. **Code Review finale**
   ```
   /code-review
   ```

## Architecture du Projet

Le projet suit une architecture en couches :

```
Web/          → Contrôleurs API, configuration
Business/     → Logique métier, services
Dal/          → Repositories, DbContext (Data Access Layer)
Dto/          → Data Transfer Objects
Bo/           → Business Objects, entités du domaine
Tests/        → Tests unitaires et d'intégration
```

Les skills respectent cette architecture et vérifient que le code est placé dans la bonne couche.

## Configuration

### Fichiers de Configuration

- `.claude/settings.local.json` : Configuration des permissions et hooks
- `.claude/skills/code-review/SKILL.md` : Définition du skill de code review
- `.claude/skills/doc-first/SKILL.md` : Définition du skill documentation-first

### Permissions

Les permissions suivantes sont pré-configurées :
- Exécution des tests (`pwsh -File run-coverage.ps1`)
- Build et test dotnet
- Migrations Entity Framework

### Hooks

Hooks configurés :
- **PostToolUse (Write/Edit)** : Rappel d'exécuter les tests après modification de code .NET

## Bonnes Pratiques

### Tests

- **Couverture minimale** : 80%
- **Couverture cible** : 90%+
- **Nomenclature** : `MethodName_Scenario_ExpectedResult`
- **Structure** : AAA (Arrange, Act, Assert)
- **Types** : Unitaires (Business), Intégration (Web), Repository (Dal)

### Documentation

- **Commentaires XML** : Obligatoires sur toutes les APIs publiques
- **Specs** : Un document par feature/changement significatif
- **README** : Maintenu à jour avec nouvelles fonctionnalités

### Sécurité

- **OWASP Top 10** : Vérification systématique
- **Validation** : Côté serveur avec attributs de validation
- **Autorisation** : `[Authorize]` sur endpoints sensibles
- **Pas de secrets** : Jamais de secrets en dur dans le code

### Qualité

- **SOLID** : Principes respectés
- **Clean Code** : Nommage explicite, méthodes courtes
- **DRY** : Pas de duplication
- **Async/Await** : Utilisé correctement pour les opérations I/O

## Désactivation Temporaire

Si vous avez besoin de désactiver temporairement les skills :

```json
// Dans .claude/settings.local.json
{
  "disableAllHooks": true
}
```

Pour désactiver un skill spécifique, vous pouvez le renommer :
```bash
mv .claude/skills/doc-first .claude/skills/doc-first.disabled
```

## Support et Questions

Si vous avez des questions sur l'utilisation des skills :
1. Demandez à Claude directement
2. Consultez ce guide
3. Lisez les fichiers SKILL.md dans `.claude/skills/`

## Exemples d'Utilisation

### Exemple 1 : Nouvelle API

```
Vous : Claude, ajoute un endpoint GET /api/events/{id} pour récupérer un événement par son ID

Claude : Je vais créer un document de conception pour ce nouvel endpoint.
         [Crée doc/specs/20260110-add-get-event-endpoint.md]
         [Présente le document avec détails sur Controller, Service, Repository, DTO]

Vous : Parfait, tu peux implémenter

Claude : [Implémente selon la doc]
         [Ajoute commentaires XML]
         [Crée les tests]
         [Exécute run-coverage.ps1]

         ✅ Implémentation terminée !
         - Couverture : 92%
         - Tests : 8 passed
```

### Exemple 2 : Code Review

```
Vous : /code-review

Claude : Je vais effectuer une code review complète.
         [Analyse les fichiers récemment modifiés]
         [Exécute les tests]
         [Génère le rapport]

         📋 Rapport de Code Review

         ✅ Points Positifs :
         - Architecture bien respectée
         - Tests complets (92% coverage)

         ⚠️ Suggestions :
         - EventsController.cs:45 : Ajouter validation pour ID négatif
         - EventService.cs:78 : Extraire méthode complexe

         🎯 Recommandation : ✅ APPROUVÉ AVEC CHANGEMENTS MINEURS
         Score : 8.5/10
```

### Exemple 3 : Bugfix

```
Vous : Il y a un bug dans la création d'événements, les dates ne sont pas validées

Claude : Je vais documenter ce bugfix avant de le corriger.
         [Crée doc/specs/20260110-fix-event-date-validation.md]

         Analyse :
         - Problème : Pas de validation des dates (date fin < date début)
         - Solution : Ajouter validation dans EventDto et EventService
         - Tests : Ajouter tests de validation

         Valider cette approche ?

Vous : Oui

Claude : [Implémente la validation]
         [Ajoute tests de non-régression]
         [Exécute run-coverage.ps1]

         ✅ Bug corrigé et tests ajoutés !
```

## Avantages

En utilisant ces skills, vous bénéficiez de :

1. **Qualité constante** : Standards appliqués systématiquement
2. **Documentation à jour** : Chaque feature est documentée
3. **Moins de bugs** : Tests et sécurité vérifiés
4. **Maintenabilité** : Architecture respectée, code clean
5. **Collaboration** : Specs claires pour toute l'équipe
6. **Traçabilité** : Historique des décisions dans doc/specs/

---

**Note** : Ces skills sont évolutifs. N'hésitez pas à les adapter à vos besoins spécifiques en modifiant les fichiers SKILL.md.
