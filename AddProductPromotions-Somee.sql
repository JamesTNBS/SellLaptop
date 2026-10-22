BEGIN TRANSACTION;
CREATE TABLE [ProductPromotions] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [DiscountPercent] decimal(5,2) NOT NULL,
    [StartsAtUtc] datetimeoffset NOT NULL,
    [EndsAtUtc] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    [IsFeatured] bit NOT NULL,
    CONSTRAINT [PK_ProductPromotions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductPromotions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ProductPromotions_ProductId] ON [ProductPromotions] ([ProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022082351_AddProductPromotions', N'10.0.7');

COMMIT;
GO
