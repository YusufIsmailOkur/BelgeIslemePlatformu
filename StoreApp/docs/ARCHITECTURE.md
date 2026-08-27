# Mimari Kararlar

Bu doküman, StoreApp'in "Belge İşleme ve Entegrasyon Platformu"na dönüşümü için alınan
temel mimari kararları özetler. Detaylı 12 haftalık plan için proje sahibindeki plan
dokümanına bakın.

## Proje Yapısı

Tek proje (`StoreApp`) korunur; `Domain`/`Infrastructure`/`Web` gibi ayrı katman
projelerine bölünmez. Mantıksal ayrım klasör/namespace ile sağlanır:

```
Controllers/
Data/                      AppDbContext + Migrations/
Models/
  Entities/                EF Core entity'leri
  Enums/                   DocumentType, DocumentStatus, RoleCode, TransformType, ...
Services/
  Abstractions/            IOcrService, IFieldExtractionService, ... arayüzler
  Jobs/                    BackgroundService implementasyonları
ViewModels/
Authorization/
Views/
wwwroot/
App_Data/uploads/          yerel dosya depolama kökü (wwwroot DIŞINDA)
TestFixtures/SampleDocuments/
```

İkinci proje olarak yalnızca `StoreApp.Tests` (xUnit) eklenir.

## Frontend

Razor MVC (server-rendered). React/TypeScript SPA'ya **geçilmez**. Etkileşimli ekranlar
(sürükle-bırak yükleme, yan yana doğrulama, alan eşleştirme, dashboard) vanilla JS/jQuery
ile progressive enhancement kullanır — Node build pipeline yoktur.

## Veritabanı

PostgreSQL, `Npgsql.EntityFrameworkCore.PostgreSQL` provider'ı ile. `AppDbContext`
(eski `RepositoryContext`) standart `AddDbContext<AppDbContext>` ile kaydedilir (pooled
factory yerine — per-request MVC controller için gereksiz ceremony).

Bağlantı dizesi: Development'ta `dotnet user-secrets`, diğer ortamlarda
`ConnectionStrings__Postgres` ortam değişkeni. `appsettings.json`'da hardcoded yol/parola
bulunmaz.

## Kimlik Doğrulama ve Yetkilendirme

Cookie tabanlı auth (`Microsoft.AspNetCore.Authentication.Cookies`) + özel `Users`/`Roles`
tabloları + `PasswordHasher<User>` (framework dahili). Tam ASP.NET Core Identity
**kullanılmaz** — Identity'nin şeması PDF kaynaklı `users.role_id` tekil FK tasarımıyla
uyuşmuyor. JWT **kullanılmaz** — SPA olmadığı için gereksiz karmaşıklık.

4 sabit rol: Operatör, Yönetici, Sistem Yöneticisi, Salt Okunur. Rol claim olarak
sign-in'de eklenir; `[Authorize(Roles="...")]` + gerekli yerlerde named policy.

## OCR ve AI Alan Çıkarımı

`IOcrService`, `IFieldExtractionService`, `IDocumentClassifierService` arayüzleri
tanımlanır. MVP boyunca **sadece mock/kural tabanlı implementasyonlar** kullanılır
(`MockOcrService`, `RuleBasedFieldExtractionService`, `MockAiFieldExtractionService`).
Gerçek Google Vision API / OpenAI API entegrasyonu, aynı arayüz üzerinden sonraki bir
fazda eklenir — çağıran kodda değişiklik gerekmez.

## Asenkron İşleme

`System.Threading.Channels.Channel<int>` (belge ID kuyruğu) + tek
`DocumentProcessingBackgroundService : BackgroundService`, `IServiceScopeFactory` ile
scoped servis çözümü. Harici broker/kuyruk sistemi (RabbitMQ, Azure Service Bus vb.)
kullanılmaz — MVP ölçeği için gereksiz.

## Dosya Depolama

`App_Data/uploads/` (config: `Storage:RootPath`), `wwwroot` dışında — `UseStaticFiles`
belgeleri asla public olarak sunmaz. Fiziksel dosya adları UUID; orijinal ad sadece
entity metadata'sı olarak saklanır. Tüm erişim `IFileStorageService` arayüzü üzerinden,
yetkili controller action'ları ile.

## Formül Transform (Alan Eşleştirme)

**NCalc** — kısıtlı ifade değerlendirici. `eval` veya dinamik kod çalıştırma
kullanılmaz.

## Grafik/Dashboard

**Chart.js**, `libman.json` ile yerelden servis edilir. Node build pipeline yoktur.

## Test Stratejisi

`StoreApp.Tests` (xUnit), Hafta 2'de kurulur. `Services/Abstractions/*` implementasyonları
için unit test; `AppDbContext`'e dokunan testler EF Core InMemory provider kullanır.
PostgreSQL'e özgü davranışlar (`jsonb`, transaction) haftalık manuel doğrulama geçişiyle
kontrol edilir (bkz. `docs/TEST-SCENARIOS.md`).
