# Changelog - ESN-WebApi

Tous les changements notables de ce projet seront documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/).

---

## [Non publié]

### 🎉 Ajouté
- Skill `/crud-generator` pour génération automatique de stack CRUD
- Skill `/performance-audit` pour détection de problèmes de performance
- Skill `/commit-message` pour messages de commit structurés
- Fichier `CLAUDE.md` comme mémoire permanente du projet
- Documentation consolidée et réorganisée

### 📝 Modifié
- Documentation réorganisée (fusion fichiers redondants)
- Guide de sécurité consolidé

### 🗑️ Supprimé
- Fichiers de documentation redondants (Securite.md, Fonctionnalites.md, etc.)

---

## [2026-01-10] - Réorganisation Documentation

### 📝 Modifié
- Création de CLAUDE.md pour mémoire persistante
- Fusion SECURITY.md + Securite.md → SECURITY.md consolidé
- Renommage Base-de-donnees.md → DATABASE.md
- Création SKILLS.md (fusion guides skills)
- Simplification README.md

### 🗑️ Supprimé
- Securite.md (fusionné dans SECURITY.md)
- Fonctionnalites.md (contenu dans README.md)
- QUICK_START.md (fusionné dans README.md)
- CORS_CONFIGURATION.md (intégré dans README.md)
- HEALTH_CHECK.md (intégré dans README.md)
- FRONTEND_INTEGRATION.md (déplacé vers projet Nuxt)
- NEW_SKILLS_SUMMARY.md (fusionné dans SKILLS.md)

---

## [Précédemment] - Développement Initial

### 🎉 Ajouté
- Architecture en couches (Web, Business, Dal, Bo, Dto, Tests)
- Authentification JWT avec refresh tokens
- Gestion utilisateurs (CRUD, rôles, permissions)
- Gestion événements (CRUD, inscriptions, capacité)
- Gestion calendriers (organisateurs multiples)
- Propositions avec système de vote
- Templates d'événements réutilisables
- Rate limiting (login, registration, voting)
- Headers de sécurité HTTP
- Validation FluentValidation
- Logging structuré (Serilog)
- Tests unitaires (MSTest + Moq)
- Script de couverture de code
- Documentation API (Swagger)
- Health checks

### 🔒 Sécurité
- Protection OWASP Top 10
- CORS configuré
- HTTPS/HSTS
- Hashage mots de passe (PBKDF2)
- Protection timing attacks
- User Secrets (dev) / Variables d'environnement (prod)

---

## Notes de Version

### Conventions de Commit

Ce projet utilise **Conventional Commits** pour les messages de commit (via skill `/commit-message`) :

- `feat`: Nouvelle fonctionnalité
- `fix`: Correction de bug
- `refactor`: Refactoring
- `perf`: Amélioration de performance
- `test`: Ajout/modification de tests
- `docs`: Documentation uniquement
- `chore`: Maintenance

---

**Note** : Ce changelog sera maintenu automatiquement via les commits structurés et les skills Claude.
