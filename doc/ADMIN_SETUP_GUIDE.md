# Guide de Configuration du Premier Admin

## 🎯 Vue d'ensemble

Ce guide explique comment créer votre premier utilisateur administrateur dans l'application ESN-WebApi.

---

## 📦 Seed Automatique

Le projet inclut un système de seed automatique (`Dal/Seeds/DatabaseSeeder.cs`) qui s'exécute au démarrage de l'application et crée :

1. **3 Rôles** :
   - **Admin** : Tous les privilèges (Create/Modify/Delete Events & Users)
   - **User** : Privilèges de base (aucun privilège spécial)
   - **Moderator** : Privilèges intermédiaires (Create/Modify Events & Users)

2. **1 Utilisateur Admin** :
   - **Email** : `admin@esn.ch`
   - **Mot de passe** : `Admin123!`
   - **Statut** : Approved (déjà approuvé)
   - **RoleId** : 1 (Admin)

### ✅ Données Déjà Créées

Une migration a déjà été appliquée pour créer ces données dans votre base de données.

---

## 🔐 Première Connexion

### Via l'API

```bash
POST /api/users/login
Content-Type: application/json

{
  "email": "admin@esn.ch",
  "password": "Admin123!"
}
```

### Via le Frontend

Accédez à la page `/login` et connectez-vous avec :
- **Email** : `admin@esn.ch`
- **Mot de passe** : `Admin123!`

---

## ⚠️ IMPORTANT : Changer le Mot de Passe

**Après votre premier login, changez immédiatement le mot de passe par défaut !**

### Via l'API

```bash
PUT /api/users/{id}/password
Authorization: Bearer {votre-token}
Content-Type: application/json

{
  "oldPassword": "Admin123!",
  "newPassword": "VotreNouveauMotDePasseSecurise123!"
}
```

### Via le Frontend

1. Connectez-vous avec le compte admin
2. Accédez à votre profil
3. Changez le mot de passe dans les paramètres

---

## 👥 Gérer les Permissions Admin

### Rôles Disponibles

| Rôle | Description | Permissions |
|------|-------------|-------------|
| **Admin** | Tous les privilèges | Tout |
| **Moderator** | Gestion des événements et users | Create/Modify Events & Users |
| **User** | Utilisateur standard | Consultation |

### Créer un Nouvel Admin

Une fois connecté en tant qu'admin, vous pouvez :

1. **Via l'interface admin** :
   - Aller sur `/admin/data/all-users`
   - Approuver un utilisateur en attente (bouton "Approve")
   - Modifier son rôle vers "Admin" (fonctionnalité à implémenter)

2. **Via l'API** :
   ```bash
   # 1. Approuver l'utilisateur
   PUT /api/users/{id}/approve
   Authorization: Bearer {admin-token}

   # 2. Modifier le rôle (endpoint à implémenter si besoin)
   PUT /api/users/{id}/role
   Authorization: Bearer {admin-token}
   Content-Type: application/json

   {
     "roleId": 1  // Admin
   }
   ```

---

## 🔧 Dépannage

### Problème : "Votre compte est en attente de validation"

**Cause** : Le statut de l'utilisateur est `Pending`.

**Solution** :
```sql
UPDATE Users
SET Status = 1  -- Approved
WHERE Email = 'votre-email@example.com';
```

### Problème : "Invalid credentials"

**Cause** : Mot de passe incorrect ou utilisateur inexistant.

**Solution** : Vérifiez l'email et réinitialisez le mot de passe si nécessaire (voir section "Réinitialiser le Mot de Passe Admin" ci-dessous).

### Problème : "Pas de rôle Admin"

**Cause** : L'utilisateur n'a pas le rôle Admin assigné.

**Solution** :
```sql
UPDATE Users
SET RoleId = 1  -- Admin
WHERE Email = 'votre-email@example.com';
```

---

## 🔄 Réinitialiser le Mot de Passe Admin

Si vous avez oublié le mot de passe admin, vous pouvez le réinitialiser via SQL.

### Étape 1 : Générer un nouveau hash

Utilisez ce code C# pour générer un hash :

```csharp
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null!, "VotreNouveauMotDePasse123!");
Console.WriteLine(hash);
```

### Étape 2 : Mettre à jour la base de données

```sql
UPDATE Users
SET PasswordHash = 'VOTRE_NOUVEAU_HASH_ICI'
WHERE Email = 'admin@esn.ch';
```

---

## 🛡️ Bonnes Pratiques de Sécurité

### ✅ À FAIRE

1. **Changez le mot de passe par défaut** immédiatement après le premier login
2. **Utilisez un mot de passe fort** (minimum 12 caractères, majuscules, minuscules, chiffres, symboles)
3. **Ne partagez jamais** les identifiants admin
4. **Créez des comptes séparés** pour chaque administrateur
5. **Supprimez le compte admin@esn.ch** après avoir créé votre propre compte admin
6. **Documentez** qui a accès au compte admin

### ❌ À NE JAMAIS FAIRE

- ❌ Laisser le mot de passe par défaut `Admin123!`
- ❌ Commit des identifiants dans Git
- ❌ Partager le compte admin entre plusieurs personnes
- ❌ Utiliser le même mot de passe pour plusieurs comptes

---

## 📋 Résumé Rapide

```bash
# Connexion
Email: admin@esn.ch
Password: Admin123!

# Changer le mot de passe immédiatement !
PUT /api/users/{id}/password
```

**⚠️ N'oubliez pas de changer le mot de passe par défaut !**

---

**Date de création** : 2026-01-11
**Dernière mise à jour** : 2026-01-11
**Version** : 2.0
