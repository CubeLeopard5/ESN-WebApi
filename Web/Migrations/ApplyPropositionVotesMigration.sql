-- Script pour appliquer la migration PropositionVotes
-- Ce script vérifie d'abord si la migration de base est enregistrée, puis applique la nouvelle migration

-- Étape 1: Vérifier et enregistrer la migration de base si nécessaire
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251026185817_UpdateEventUserIdColumn')
BEGIN
    -- La migration de base existe déjà dans la DB mais n'est pas enregistrée dans l'historique
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251026185817_UpdateEventUserIdColumn', N'9.0.10');
    PRINT 'Migration de base enregistrée dans l''historique';
END

-- Étape 2: Appliquer la migration PropositionVotes
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251221172509_AddPropositionVotesManual')
BEGIN
    BEGIN TRANSACTION;

    -- Créer la table PropositionVotes
    CREATE TABLE [PropositionVotes] (
        [Id] int NOT NULL IDENTITY,
        [PropositionId] int NOT NULL,
        [UserId] int NOT NULL,
        [VoteType] int NOT NULL,
        [CreatedAt] datetime NOT NULL DEFAULT ((getdate())),
        [UpdatedAt] datetime NULL,
        CONSTRAINT [PK__PropositionVotes__3214EC07] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropositionVotes_Propositions] FOREIGN KEY ([PropositionId]) REFERENCES [Propositions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PropositionVotes_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );

    -- Créer les index
    CREATE INDEX [IX_PropositionVotes_PropositionId] ON [PropositionVotes] ([PropositionId]);
    CREATE UNIQUE INDEX [UQ_PropositionVotes_User_Proposition] ON [PropositionVotes] ([UserId], [PropositionId]);

    -- Enregistrer la migration
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251221172509_AddPropositionVotesManual', N'10.0.1');

    COMMIT;

    PRINT 'Table PropositionVotes créée avec succès';
END
ELSE
BEGIN
    PRINT 'La migration PropositionVotes est déjà appliquée';
END
GO
