-- Run on the Somee SQL Server database after the AddProductPromotions migration.
-- The first run sets every existing product to 10. New products default to 0,
-- so Admin must enter the actual quantity. Re-running this script preserves stock edits.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @StockWasInitialized bit = 0;
IF EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027184246_SetProductStockDefaultToTen'
)
    SET @StockWasInitialized = 1;

IF COL_LENGTH(N'dbo.Products', N'StockQuantity') IS NULL
BEGIN
    DECLARE @AddStockColumnSql nvarchar(4000) =
        N'ALTER TABLE [dbo].[Products] ADD [StockQuantity] int NOT NULL CONSTRAINT [DF_Products_StockQuantity] DEFAULT (10);';
    EXEC sys.sp_executesql @AddStockColumnSql;
END;

IF @StockWasInitialized = 0
BEGIN
    DECLARE @InitializeStockSql nvarchar(4000) =
        N'UPDATE [dbo].[Products] SET [StockQuantity] = 10;';
    EXEC sys.sp_executesql @InitializeStockSql;
END;

DECLARE @DefaultConstraint sysname;
SELECT @DefaultConstraint = dc.[name]
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c
    ON c.[default_object_id] = dc.[object_id]
WHERE dc.[parent_object_id] = OBJECT_ID(N'dbo.Products')
  AND c.[name] = N'StockQuantity';

IF @DefaultConstraint IS NOT NULL
BEGIN
    DECLARE @DropDefaultConstraintSql nvarchar(4000);
    SET @DropDefaultConstraintSql =
        N'ALTER TABLE [dbo].[Products] DROP CONSTRAINT ' + QUOTENAME(@DefaultConstraint) + N';';
    EXEC sys.sp_executesql @DropDefaultConstraintSql;
END;

DECLARE @SetStockDefaultSql nvarchar(4000) =
    N'ALTER TABLE [dbo].[Products] ADD CONSTRAINT [DF_Products_StockQuantity] DEFAULT (0) FOR [StockQuantity];';
EXEC sys.sp_executesql @SetStockDefaultSql;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027180829_AddProductStockQuantity'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027180829_AddProductStockQuantity', N'10.0.7');
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027184246_SetProductStockDefaultToTen'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027184246_SetProductStockDefaultToTen', N'10.0.7');
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027184456_SetNewProductStockDefaultToZero'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027184456_SetNewProductStockDefaultToZero', N'10.0.7');
END;

COMMIT;
GO
