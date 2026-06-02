# 🐍 Slither Dungeon (v1.0.0 - Master Edition)

Windows Forms altyapısı ve **C#** programlama dili kullanılarak, harici hiçbir oyun motoru (Unity, Unreal vb.) kullanılmadan tamamen yerel grafik kütüphaneleriyle geliştirilmiş **70 bölümlük, dinamik bir zindan tırmanış ve hayatta kalma oyunudur.**

---

## 🎮 Oyunun Amacı ve İşleyişi (Gameplay Loop)
Klasik yılan oyunlarındaki sonsuz skor mantığının aksine, oyunumuzda oyuncu **7 farklı lige (Tier)** yayılmış toplam **70 odayı (bölümü)** temizleyerek zindanın zirvesine tırmanmaya çalışır. 

Her lig 10 odadan oluşur ve kendine has bir atmosfer rengine, hıza ve engel yoğunluğuna sahiptir:
* **Demir Ligi (Lvl 1-10):** Başlangıç aşaması, harita boştur.
* **Bronz Ligi (Lvl 11-20):** Haritaya sabit taş duvar engelleri eklenir.
* **Gümüş Ligi (Lvl 21-30):** Harita sınırlarından fiziksel vektörlerle seken yapay zekaya sahip **Avcı Kartal** düşmanı zindana dahil olur.
* **Altın Ligi (Lvl 31-40):** Oyun hızı artar, duvar sayıları yoğunlaşır.
* **Platin Ligi (Lvl 41-50):** Reflekslerin ölçüldüğü yüksek hız seviyesi.
* **Zümrüt Ligi (Lvl 51-60):** Yoğun engel dizilimleri ve dar hareket alanları.
* **Elmas Ligi (Lvl 61-70):** Maksimum hız, 40 adet dinamik duvar engeli ve en yüksek zorluk seviyesi.

---

## 🎯 Yenilikçi Mekanikler

### 🔄 Tersine Azalan Zorluk Eğrisi (Reverse Difficulty Curve)
Oyun ilerledikçe yılan ışık hızına ulaştığı ve harita engellerle dolduğu için, alan daralmaktadır. Bu algoritmik zorluğu dengelemek amacıyla dâhiyane bir **Ters Eğri** kurgulanmıştır:
* **Seviye 1'de** temizlik için tam **30 yem** hedefi istenirken;
* **Seviye 70'e** gelindiğinde o cehennem odasında hayatta kalıp sadece **1 veya 2 yem** toplamak bölümü geçmek için yeterli olmaktadır.

### 💰 Ekonomi ve Mağaza Sistemi
Yenilen her yem oyuncuya **+10 Altın** kazandırır. Toplanan bu altınlar ana menüdeki modern kart tasarımlı **Zindan Mağazası** ekranında harcanarak karakter için kozmetik özelleştirmeler (**Kral Tacı** veya asil siyah-beyaz **Asil Kostüm**) satın almak ve kuşanmak için kullanılır.

---

## 🛠️ Kullanılan Teknik Teknolojiler ve Algoritmalar

* **Çizim Motoru (GDI+):** Ekrandaki tüm görseller, duvarlar, yılan parçaları ve yem efektleri `System.Drawing` ve `System.Drawing.Drawing2D` kütüphaneleri kullanılarak pikseller halinde dinamik olarak ekrana çizdirilmiştir (`OnPaint` ve `DoubleBuffered` performansı ile titreme engellenmiştir).
* **Veri Yapısı (List<Point>):** Yılanın gövdesi koordinat tabanlı bir dinamik liste üzerinde tutulmuştur. Hareket algoritması, listenin başına yeni kafa koordinatını ekleyip (`Insert`), yem yenmediği sürece son elemanı silmek (`RemoveAt`) üzerine kurgulanarak **Kuyruk (Queue - FIFO)** mantığı simüle edilmiştir.
* **Klavye Dinleyicisi (ProcessCmdKey Override):** İşletim sisteminden gelen klavye girdileri form düzeyinde ezilerek (override) **WASD** ve **Yön Tuşları** anlık olarak yakalanmış, yılanın kendi içine kırılması algoritmik olarak engellenmiştir.
* **Kayıt Sistemi (File I/O):** Oyuncunun kazandığı altınlar, açtığı maksimum seviye ve mağaza envanteri Windows'un güvenli `AppData\Roaming\SlitherDungeon` klasörü altında şifreli bir biçimde diske yazılır ve oyun açılırken otomatik yüklenir.
* **Geliştirici Test Modu (Cheat Code):** Projenin test süreçleri ve tanıtım videoları için oyun esnasında **[P]** tuşuna basıldığında o bölümü anında temizleyen gizli bir geliştirici hilesi entegre edilmiştir.

---

## 📦 Kurulum ve Çalıştırma
Proje, son kullanıcı deneyimi standartlarında **Inno Setup 6** ile profesyonel bir yükleme paketine dönüştürülmüştür. 
1. Sağ taraftaki **Releases** sekmesine tıklayın.
2. `SlitherDungeon_Setup.exe` dosyasını indirin ve kurun.
3. Masaüstünüze gelen özel yılan ikonlu kısayola tıklayarak zindana adım atın!
