BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.Products', N'TechnicalSpecifications') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [TechnicalSpecifications] nvarchar(max) NOT NULL CONSTRAINT [DF_Products_TechnicalSpecifications] DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009030552_AddTechnicalSpecifications'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009030552_AddTechnicalSpecifications', N'10.0.7');
END;
COMMIT;EXEC sys.sp_executesql N'SET NOCOUNT ON;

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: AMD Radeon Graphics (integrated)
Operating system upon release: Windows 11 Home (current listing)
CPU type: AMD Ryzen 5 5500U, 6 cores / 12 threads, 2.1-4.0 GHz

RAM
RAM capacity: 8GB
RAM type: DDR4-3200
Number of RAM slots: One soldered memory module and one DDR4 SO-DIMM slot (model family)
Storage: 512GB SSD (listing configuration)

Screen
Display technology: Full HD display (listing configuration)
Screen size: 15.6 inches
Screen resolution: Full HD (listing configuration)

Connection port
Communication port: USB 2.0, USB 3.2 Gen 1 Type-A, USB-C 3.2 Gen 1 (data transfer), HDMI, card reader, 3.5mm headphone/microphone combo'' WHERE Id = 27 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: Intel Iris Xe Graphics
CPU type: Intel Core i7-1165G7, 4 cores / 8 threads, up to 4.7 GHz

RAM
RAM capacity: 16GB (listing configuration)
RAM type: LPDDR4x-4267 (model family)
Storage: 512GB SSD (listing configuration)

Screen
Display technology: WVA, anti-reflective, 500 nits
Screen size: 13.4 inches
Screen resolution: 3840 x 2400 (UHD+), touch
Scanning frequency: 60Hz

Connection port
Communication port: 2 x Thunderbolt 4 USB-C with Power Delivery, 3.5mm headset jack, microSD card slot'' WHERE Id = 28 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: Intel Iris Xe Graphics (dual-channel memory configuration)
Operating system upon release: Windows 11 (current listing)
CPU type: Intel Core i5-1235U, 10 cores / 12 threads, up to 4.4 GHz

RAM
RAM capacity: 8GB
RAM type: DDR4-3200
Number of RAM slots: 2 SO-DIMM slots
Storage: 512GB SSD (listing configuration)

Screen
Screen size: 15.6 inches
Screen resolution: Full HD (listing configuration)

Other features
Security: Fingerprint reader'' WHERE Id = 29 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: AMD Radeon Vega Graphics (integrated)
Operating system upon release: Windows 10 (current listing)
CPU type: AMD Ryzen 3 3200U (listing configuration)

RAM
RAM capacity: 4GB (listing configuration)
Storage: 256GB SSD (listing configuration)

Screen
Screen size: 15.6 inches
Screen resolution: HD (listing configuration)'' WHERE Id = 30 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: Apple M3 Max GPU (30-core or 40-core, configuration dependent)
CPU type: Apple M3 Max (14-core or 16-core CPU, configuration dependent)

RAM
RAM capacity: 36GB unified memory (listing configuration)
Storage: 1TB SSD (listing configuration)

Screen
Display technology: Liquid Retina XDR, P3 wide color, ProMotion up to 120Hz
Screen size: 16.2 inches
Screen resolution: 3456 x 2234
Scanning frequency: Up to 120Hz

Batteries & Charging Technology
Battery: 100Wh lithium-polymer; up to 22 hours video playback (Apple-rated)

Connection port
Communication port: 3 x Thunderbolt 4 USB-C, HDMI, SDXC card slot, MagSafe 3, 3.5mm headphone jack'' WHERE Id = 32 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: NVIDIA GeForce RTX 4060 Laptop GPU, 8GB GDDR6
CPU type: Intel Core i7-13800H, 13th Gen

RAM
RAM capacity: 16GB (listing configuration)
RAM type: LPDDR5x
Storage: 512GB Gen 4 SSD (listing configuration)

Screen
Display technology: PixelSense Flow touchscreen, Dolby Vision IQ
Screen size: 14.4 inches
Screen resolution: 2400 x 1600
Scanning frequency: Up to 120Hz

Connection port
Communication port: Thunderbolt 4 USB-C, USB-A, 3.5mm headphone jack, Surface Connect'' WHERE Id = 33 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

UPDATE dbo.Products SET TechnicalSpecifications = N''Configuration & Memory
Graphics card type: NVIDIA GeForce RTX 4090 Laptop GPU
CPU type: Intel Core i9-13900H, 14 cores / 20 threads, up to 5.4 GHz

RAM
RAM capacity: 32GB (listing configuration)
RAM type: DDR5-5200 (model family)
Number of RAM slots: 2 SO-DIMM slots, up to 64GB (model family)
Storage: 2TB SSD (listing configuration); 2 x M.2 PCIe Gen 4 slots (model family)

Screen
Display technology: IPS-Level
Screen size: 16 inches
Screen resolution: 3840 x 2400 (UHD+), 120Hz (model family)
Scanning frequency: 120Hz

Sound
Audio technology: 6-speaker Dynaudio system

Batteries & Charging Technology
Battery: 99.9Wh, 4-cell

Communication & Connection
Wi-Fi: Intel Killer Wi-Fi 6E
Bluetooth: Bluetooth 5.3

Connection port
Communication port: RJ45, USB-C 3.2 Gen 2 with DisplayPort, Thunderbolt 4, USB-A 3.2 Gen 2, HDMI 2.1, microSD card reader, 3.5mm combo audio jack'' WHERE Id = 35 AND (TechnicalSpecifications IS NULL OR LTRIM(RTRIM(TechnicalSpecifications)) = N'''');

SELECT Id, LEN(TechnicalSpecifications) AS TechnicalSpecLength FROM dbo.Products ORDER BY Id;
';
