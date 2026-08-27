# MVP Kapsamı

## 90 Gün Sonunda Ortaya Çıkacak Ürün

PDF, Excel ve CSV formatındaki ticari/operasyonel belgeleri yükleyebilen, ham metin veya
tablo verisi çıkarabilen, temel alanları yapılandırılmış veriye dönüştüren, kullanıcı
doğrulaması alabilen ve onaylanan veriyi SQL'e kaydedip CSV/Excel/JSON olarak dışa
aktarabilen web tabanlı bir MVP.

## Olmazsa Olmaz Özellikler (MVP'de Dahil)

- PDF, Excel ve CSV yükleme (tekil + toplu)
- Belge listesi ve işlem durumu takibi
- Dijital PDF metin çıkarma
- Taranmış PDF için OCR (mock/kural tabanlı implementasyon)
- Excel/CSV tablo okuma
- Temel belge türü seçimi (manuel + basit otomatik öneri)
- Fatura/teklif/sipariş/irsaliye/genel belge için ortak alan çıkarımı
- Yan yana doğrulama ekranı, kullanıcı düzeltmesi ve onayı
- SQL kayıt, audit trail, soft delete
- Alan eşleştirme (direkt aktarım, lookup, format dönüşümü, sabit değer, basit formül)
- CSV/Excel/JSON export
- Dashboard (KPI, belge türü dağılımı, hata oranı, OCR/alan güveni)
- Cookie tabanlı auth ve rol bazlı yetkilendirme (Operatör, Yönetici, Sistem Yöneticisi,
  Salt Okunur)

## MVP Dışında Bırakılan Özellikler (Bilinçli Kapsam Dışı)

Bu proje 90 günde tek/küçük ekip tarafından geliştirilebilir, ancak bunun için kapsam
disiplinli tutulmalıdır. Aşağıdakiler **kasıtlı olarak MVP dışı bırakılmıştır**:

- E-posta entegrasyonu (mail eklerinden otomatik belge çekme)
- Klasör izleme (belirli bir klasöre düşen dosyaları otomatik işleme)
- Gelişmiş/öğrenen belge şablonları (müşteri bazlı şablon öğrenme)
- Tam otomatik ERP connector'ları (sadece CSV/Excel/JSON export + opsiyonel SQL staging
  tablosu desteklenir)
- Çoklu müşteri (multi-tenant) mimarisi
- Gelişmiş koşullu formül motoru (sadece NCalc ile basit, güvenli ifadeler)
- Mobil onay uygulaması
- Gelişmiş KVKK süreç yönetimi (yalnızca temel güvenlik: yetki kontrolü, audit log,
  güvenli dosya erişimi)
- Yüksek ölçekli queue/worker mimarisi (in-process `Channel` + `BackgroundService`
  yeterli kabul edilir)
- Marketplace tipi entegrasyon şablon kütüphanesi

## Faz 2'de Değerlendirilecekler

Hazır ERP entegrasyonları, e-posta eklerinden otomatik belge çekme, klasör izleme,
müşteri bazlı şablon öğrenme, gelişmiş raporlama, API key yönetimi, bildirim sistemi,
çoklu entegrasyon profilleri, gerçek Google Vision API / OpenAI API entegrasyonu (MVP'de
mock/kural tabanlı).

## Gerçekçi Sınırlar

Bu ürünün amacı her belgeyi kusursuz anlamak değildir. Sistem ham veri çıkarır, makul bir
ilk taslak oluşturur, kullanıcıya doğrulatır, onaylanan veriyi kaydeder ve hedef sisteme
aktarılabilir formatta üretir. Gerçekçilik, kahramanlıktan daha değerlidir.
