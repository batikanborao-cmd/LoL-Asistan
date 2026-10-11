<p align="center"><img src="src/icon.png" width="128" alt="LoL Asistan"></p>

<h1 align="center">LoL Asistan</h1>

League of Legends için küçük bir Windows yardımcısı. Açık kaldığı sürece:

- **Maçı otomatik kabul eder.**
- **Sırayla seçer:** Yazdığın 3 şampiyondan ilk uygun olanı seçer. Banlanmış, alınmış ya da takım arkadaşının hover'ladığı şampiyonu atlar. İstersen kilitler, istersen sadece hover'lar.
- **Sırayla banlar:** Yazdığın 3 şampiyondan ilk uygun olanı banlar.
- **ARAM:** Seçim listendeki şampiyonlardan biri bench'e düşerse, listedeki sıraya göre en üsttekini alır.

## İndirme

1. **Releases** bölümünden en son `LoL Asistan.exe` dosyasını indir.
2. Çift tıklayıp aç. Kurulum gerekmez.
3. Windows "Bilgisayarınız korundu" uyarısı verirse **Ek bilgi → Yine de çalıştır** de. Uygulama imzalı olmadığı için bu uyarı normal.

## Kullanım

- Şampiyon adını yazmaya başla, öneri listesi açılır. Kesme işaretine, boşluğa ve küçük yazım hatalarına takılmaz: `kaisa` → Kai'Sa, `xearth` → Xerath, `mf` → Miss Fortune.
- Kutunun yanında **✓ isim** görüyorsan doğru anlaşılmış demektir.
- **Asistan açık** kutusunun işaretini kaldırırsan hiçbir şey yapmaz, sadece durumu izler.
- Ayarlar `%APPDATA%\LolAsistan` klasörüne kaydedilir.
- LoL'ü uygulamadan önce ya da sonra açman fark etmez, kendi kendine bağlanır.

## Nasıl çalışır

LoL istemcisinin bilgisayarında açtığı yerel API'yi (LCU) kullanır, yani istemcinin kendi butonlarına basmakla aynı işi yapar. Oyunun kendisine dokunmaz, internete sadece şampiyon listesini almak için (Riot Data Dragon) bağlanır.

## Kaynaktan derleme

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Windows'ta hazır gelen .NET Framework 4 derleyicisini kullanır, ek bir şey kurman gerekmez. Çıktı: `out\LoL Asistan.exe`.

## Uyarı

Bu proje Riot Games ile bağlantılı değildir ve Riot tarafından onaylanmamıştır. Otomatik kabul ve seçim yapan araçlar (Porofessor, Blitz vb.) yaygın olarak kullanılıyor, yine de kullanım sorumluluğu sende.
