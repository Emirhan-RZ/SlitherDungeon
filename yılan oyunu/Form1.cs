using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace yılan_oyunu
{
    public partial class Form1 : Form
    {
        // ==========================================
        // OYUN DURUMLARI (STATE MANAGEMENT)
        // ==========================================
        private enum GameState { Menu, LevelSelect, Playing, Shop, HowToPlay, GameOver, LevelComplete }
        private GameState currentState = GameState.Menu;

        // UI Elemanları
        private Button btnStart, btnShop, btnHowToPlay, btnExit;
        private Button btnBuyCrown, btnBuyAsilSkin;
        private Button btnBackFromShop, btnBackFromLevels, btnBackFromHow;

        // Sabitler ve Ayarlar
        private const int CellSize = 25;
        private List<Point> snake = new List<Point>();
        private List<Point> walls = new List<Point>();
        private Point direction = new Point(1, 0);
        private Point nextDirection = new Point(1, 0);
        private Point food = new Point(5, 5);

        // İlerleme Sistemi (Progression System)
        private int currentActiveLevel = 1;   // Şu an oynanan aktif bölüm
        private int unlockedMaxLevel = 1;     // Kayıt dosyasından yüklenen, açılmış maks bölüm
        private int foodsEatenInLevel = 0;
        private int targetFoodsToClear = 10;
        private int levelTransitionCounter = 0;

        // Ekonomi ve Süre
        private Timer gameTimer = new Timer();
        private long levelStartTimeTicks;
        private int levelTimeInSeconds = 0;
        private int totalGold = 0;
        private const int GoldPerFood = 10;

        // Düşman Avcı Kartal
        private Point eaglePos = new Point(-1, -1);
        private Point eagleVelocity = new Point(1, 1);
        private bool isEagleActive = false;
        private int eagleTickCounter = 0;
        private const int EagleMoveDelay = 3;

        // Kayıt ve Görsel Değişkenler
        private string saveFilePath;
        private bool hasCrown = false, hasAsilSkin = false;
        private bool equipCrown = false, equipAsilSkin = false;

        private Color zindanColor = Color.FromArgb(15, 15, 15);
        private Color yemColor = Color.Crimson;
        private string tierName = "DEMİR";
        private int flashAlpha = 0;
        private Image menuBg;
        private Random rnd = new Random();

        // Pencere Sürükleme Mekanizması
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public Form1()
        {
            InitializeComponent();

            // AppData veya Yerel Belgeler klasörüne kayıt ederek Windows izin hatalarını önlüyoruz
            string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlitherDungeon");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            saveFilePath = Path.Combine(appFolder, "slither_dungeon_v3.data");

            LoadGameProgress(); // Açılışta kaydı yükle (unlockedMaxLevel artık güvende)
            SetupFormLayout();
            InitializeControls();
        }

        private void SetupFormLayout()
        {
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(10, 10, 15);
            this.ClientSize = new Size(800, 600);
            this.Text = "Slither Dungeon";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            try { menuBg = Image.FromFile(Path.Combine(Application.StartupPath, @"img\yılan.png")); }
            catch { }

            gameTimer.Tick += GameLoopTick;
        }

        protected override void OnMouseDown(MouseEventArgs e) { dragging = true; dragCursorPoint = Cursor.Position; dragFormPoint = this.Location; base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (dragging) { Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint)); this.Location = Point.Add(dragFormPoint, new Size(dif)); } base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; base.OnMouseUp(e); }

        private void InitializeControls()
        {
            Font btnFont = new Font("Segoe UI Semibold", 14);
            Color bgNormal = Color.FromArgb(200, 20, 20, 20);
            Color bgHover = Color.FromArgb(230, 50, 50, 50);

            // --- DEĞİŞEN KISIM: Direkt başlatmak yerine haritaya yönlendiriyor ---
            btnStart = CreateButton("OYUNA BAŞLAT", 300, 250, Color.LimeGreen, btnFont, bgNormal, bgHover);
            btnStart.Click += (s, e) => { ManageUIState(GameState.LevelSelect); };

            btnShop = CreateButton("MAĞAZA", 300, 310, Color.Gold, btnFont, bgNormal, bgHover);
            btnShop.Click += (s, e) => { ManageUIState(GameState.Shop); };

            btnHowToPlay = CreateButton("NASIL OYNANIR?", 300, 370, Color.LightSkyBlue, btnFont, bgNormal, bgHover);
            btnHowToPlay.Click += (s, e) => { ManageUIState(GameState.HowToPlay); };

            btnExit = CreateButton("ÇIKIŞ", 300, 430, Color.Crimson, btnFont, bgNormal, bgHover);
            btnExit.Click += (s, e) => { SaveGameProgress(); Application.Exit(); };

            Font shopBtnFont = new Font("Segoe UI", 11, FontStyle.Bold);
            btnBuyCrown = CreateButton("KRAL TACI", 125, 370, Color.Gold, shopBtnFont, bgNormal, bgHover, 200);
            btnBuyCrown.Click += (s, e) => HandleCrownAction();

            btnBuyAsilSkin = CreateButton("ASİL KOSTÜM", 475, 370, Color.White, shopBtnFont, bgNormal, bgHover, 200);
            btnBuyAsilSkin.Click += (s, e) => HandleAsilAction();

            btnBackFromShop = CreateButton("MENÜYE DÖN", 300, 480, Color.Gray, btnFont, bgNormal, bgHover);
            btnBackFromShop.Click += (s, e) => { SaveGameProgress(); ManageUIState(GameState.Menu); };

            btnBackFromLevels = CreateButton("MENÜYE DÖN", 300, 520, Color.Gray, btnFont, bgNormal, bgHover);
            btnBackFromLevels.Click += (s, e) => { ManageUIState(GameState.Menu); };

            btnBackFromHow = CreateButton("MENÜYE DÖN", 300, 480, Color.Gray, btnFont, bgNormal, bgHover);
            btnBackFromHow.Click += (s, e) => { ManageUIState(GameState.Menu); };

            ManageUIState(GameState.Menu);
        }

        private Button CreateButton(string txt, int x, int y, Color textCol, Font f, Color bg, Color hov, int width = 200)
        {
            Button b = new Button { Text = txt, Size = new Size(width, 50), Location = new Point(x, y), Font = f, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = textCol, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => { b.BackColor = hov; b.ForeColor = Color.White; };
            b.MouseLeave += (s, e) => { b.BackColor = bg; b.ForeColor = textCol; };
            this.Controls.Add(b);
            return b;
        }

        private void ManageUIState(GameState state)
        {
            currentState = state;
            bool isMenu = (state == GameState.Menu);
            bool isShop = (state == GameState.Shop);
            bool isLevels = (state == GameState.LevelSelect);
            bool isHow = (state == GameState.HowToPlay);

            btnStart.Visible = btnShop.Visible = btnHowToPlay.Visible = btnExit.Visible = isMenu;
            btnBuyCrown.Visible = btnBuyAsilSkin.Visible = btnBackFromShop.Visible = isShop;
            btnBackFromLevels.Visible = isLevels;
            btnBackFromHow.Visible = isHow;

            if (isShop) UpdateShopButtonVisuals();

            this.Focus(); this.Invalidate();
        }

        private void SaveGameProgress() { try { File.WriteAllText(saveFilePath, $"{totalGold},{unlockedMaxLevel},{hasCrown},{hasAsilSkin},{equipCrown},{equipAsilSkin}"); } catch { } }
        private void LoadGameProgress()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string[] data = File.ReadAllText(saveFilePath).Split(',');
                    totalGold = int.Parse(data[0]);
                    unlockedMaxLevel = Math.Max(1, int.Parse(data[1])); // Kalınan aşama başarıyla yüklenir
                    hasCrown = bool.Parse(data[2]);
                    hasAsilSkin = bool.Parse(data[3]);
                    equipCrown = bool.Parse(data[4]);
                    equipAsilSkin = bool.Parse(data[5]);
                }
                catch { }
            }
        }

        private void UpdateShopButtonVisuals()
        {
            if (hasCrown) btnBuyCrown.Text = equipCrown ? "[KUŞANILDI]" : "KUŞAN";
            else btnBuyCrown.Text = "SATIN AL (500G)";

            if (hasAsilSkin) btnBuyAsilSkin.Text = equipAsilSkin ? "[KUŞANILDI]" : "KUŞAN";
            else btnBuyAsilSkin.Text = "SATIN AL (1903G)";
        }

        private void HandleCrownAction()
        {
            if (!hasCrown && totalGold >= 500) { totalGold -= 500; hasCrown = true; equipCrown = true; }
            else if (hasCrown) { equipCrown = !equipCrown; if (equipCrown) equipAsilSkin = false; }
            UpdateShopButtonVisuals(); SaveGameProgress(); this.Invalidate();
        }

        private void HandleAsilAction()
        {
            if (!hasAsilSkin && totalGold >= 1903) { totalGold -= 1903; hasAsilSkin = true; equipAsilSkin = true; }
            else if (hasAsilSkin) { equipAsilSkin = !equipAsilSkin; if (equipAsilSkin) equipCrown = false; }
            UpdateShopButtonVisuals(); SaveGameProgress(); this.Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (currentState == GameState.LevelSelect)
            {
                int startX = 50, startY = 120, colSize = 65, rowSize = 50;
                int clickedCol = (e.X - startX) / colSize;
                int clickedRow = (e.Y - startY) / rowSize;

                if (clickedCol >= 0 && clickedCol < 10 && clickedRow >= 0 && clickedRow < 7)
                {
                    int clickedLevel = (clickedRow * 10) + clickedCol + 1;
                    if (clickedLevel <= unlockedMaxLevel) { currentActiveLevel = clickedLevel; SetupAndStartLevel(); }
                }
            }
            base.OnMouseClick(e);
        }

        private void SetupAndStartLevel()
        {
            ManageUIState(GameState.Playing);
            snake.Clear(); walls.Clear();
            snake.Add(new Point(10, 10)); snake.Add(new Point(9, 10)); snake.Add(new Point(8, 10));
            direction = nextDirection = new Point(1, 0);
            foodsEatenInLevel = 0; flashAlpha = 0; levelTransitionCounter = 0;

            LoadTierSettings();
            GenerateFoodPosition();

            levelStartTimeTicks = DateTime.Now.Ticks;
            levelTimeInSeconds = 0;
            gameTimer.Start(); this.Invalidate();
        }

        private void LoadTierSettings()
        {
            int wallToGen = 0; isEagleActive = false;
            // 1. Bölümde hedef 30 yem, 70. Bölüme gelindiğinde hedef 1 yeme kadar düşer!
            targetFoodsToClear = 31 - (int)Math.Ceiling(currentActiveLevel / 2.33);
            if (targetFoodsToClear < 1) targetFoodsToClear = 1; // 1'in altına düşmesini engelleriz

            if (currentActiveLevel <= 10)
            {
                tierName = "DEMİR"; yemColor = Color.LightSlateGray; zindanColor = Color.FromArgb(20, 20, 25);
                gameTimer.Interval = 75; wallToGen = 0;
            }
            else if (currentActiveLevel <= 20)
            {
                tierName = "BRONZ"; yemColor = Color.OrangeRed; zindanColor = Color.FromArgb(25, 20, 15);
                gameTimer.Interval = 70; wallToGen = 5;
            }
            else if (currentActiveLevel <= 30)
            {
                tierName = "GÜMÜŞ"; yemColor = Color.FromArgb(200, 200, 200); zindanColor = Color.FromArgb(15, 20, 25);
                gameTimer.Interval = 65; wallToGen = 10; ActivateHunterEagle();
            }
            else if (currentActiveLevel <= 40)
            {
                tierName = "ALTIN"; yemColor = Color.Gold; zindanColor = Color.FromArgb(25, 25, 10);
                gameTimer.Interval = 60; wallToGen = 14; ActivateHunterEagle();
            }
            else if (currentActiveLevel <= 50)
            {
                tierName = "PLATİN"; yemColor = Color.Cyan; zindanColor = Color.FromArgb(10, 25, 25);
                gameTimer.Interval = 55; wallToGen = 18; ActivateHunterEagle();
            }
            else if (currentActiveLevel <= 60)
            {
                tierName = "ZÜMRÜT"; yemColor = Color.LimeGreen; zindanColor = Color.FromArgb(10, 30, 15);
                gameTimer.Interval = 50; wallToGen = 22; ActivateHunterEagle();
            }
            else
            {
                tierName = "ELMAS"; yemColor = Color.DeepSkyBlue; zindanColor = Color.FromArgb(10, 10, 30);
                gameTimer.Interval = 45; wallToGen = 28; ActivateHunterEagle();
            }

            for (int i = 0; i < wallToGen; i++) GenerateWallStructure();
        }

        private void ActivateHunterEagle()
        {
            isEagleActive = true; eagleTickCounter = 0;
            eaglePos = new Point(this.ClientSize.Width / CellSize - 5, this.ClientSize.Height / CellSize - 5);
            eagleVelocity = new Point(-1, -1);
        }

        private void GameLoopTick(object sender, EventArgs e)
        {
            if (currentState == GameState.LevelComplete)
            {
                levelTransitionCounter++;
                if (levelTransitionCounter > 25)
                {
                    // --- DÜZELTİLEN KISIM: Seviye atlayınca kilit mekanizmasını doğru güncelliyor ---
                    if (currentActiveLevel == unlockedMaxLevel)
                    {
                        unlockedMaxLevel++;
                    }
                    currentActiveLevel++;

                    gameTimer.Stop();
                    SaveGameProgress();
                    ManageUIState(GameState.LevelSelect); // Başarı ekranından sonra haritaya atar
                }
                this.Invalidate(); return;
            }

            if (currentState != GameState.Playing) return;
            if (flashAlpha > 0) { flashAlpha -= 20; if (flashAlpha < 0) flashAlpha = 0; }

            long elapsedTicks = DateTime.Now.Ticks - levelStartTimeTicks;
            levelTimeInSeconds = (int)(elapsedTicks / TimeSpan.TicksPerSecond);

            direction = nextDirection;
            Point newHeadPos = new Point(snake[0].X + direction.X, snake[0].Y + direction.Y);

            if (newHeadPos.X < 0 || newHeadPos.X >= this.ClientSize.Width / CellSize ||
                newHeadPos.Y < 1 || newHeadPos.Y >= this.ClientSize.Height / CellSize ||
                snake.Contains(newHeadPos) || walls.Contains(newHeadPos))
            {
                flashAlpha = 220; EndGameDungeon(); return;
            }

            if (isEagleActive && (newHeadPos == eaglePos || snake[0] == eaglePos))
            {
                flashAlpha = 255; EndGameDungeon(); return;
            }

            snake.Insert(0, newHeadPos);

            if (newHeadPos == food)
            {
                totalGold += GoldPerFood;
                foodsEatenInLevel++;

                if (foodsEatenInLevel >= targetFoodsToClear)
                {
                    totalGold += (currentActiveLevel * 5);
                    currentState = GameState.LevelComplete;
                }
                else { GenerateFoodPosition(); }
            }
            else { snake.RemoveAt(snake.Count - 1); }

            if (isEagleActive)
            {
                eagleTickCounter++;
                if (eagleTickCounter >= EagleMoveDelay)
                {
                    eagleTickCounter = 0;
                    eaglePos.X += eagleVelocity.X; eaglePos.Y += eagleVelocity.Y;

                    if (eaglePos.X <= 0 || eaglePos.X >= (this.ClientSize.Width / CellSize) - 1) eagleVelocity.X *= -1;
                    if (eaglePos.Y <= 1 || eaglePos.Y >= (this.ClientSize.Height / CellSize) - 1) eagleVelocity.Y *= -1;

                    if (walls.Contains(eaglePos)) { eagleVelocity.X *= -1; eagleVelocity.Y *= -1; }
                }
            }

            this.Invalidate();
        }

        private void GenerateFoodPosition()
        {
            do { food = new Point(rnd.Next(1, this.ClientSize.Width / CellSize - 1), rnd.Next(2, this.ClientSize.Height / CellSize - 1)); }
            while (snake.Contains(food) || walls.Contains(food) || food == eaglePos);
        }

        private void GenerateWallStructure()
        {
            Point blockPos;
            do { blockPos = new Point(rnd.Next(2, this.ClientSize.Width / CellSize - 2), rnd.Next(2, this.ClientSize.Height / CellSize - 2)); }
            while (snake.Contains(blockPos) || blockPos == food || walls.Contains(blockPos) || Math.Abs(blockPos.X - 10) < 3);
            walls.Add(blockPos);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (currentState == GameState.Playing) { flashAlpha = 150; EndGameDungeon(); }
                else if (currentState == GameState.LevelSelect || currentState == GameState.Shop || currentState == GameState.HowToPlay) { ManageUIState(GameState.Menu); }
                else if (currentState == GameState.GameOver) { ManageUIState(GameState.LevelSelect); }
                return true;
            }

            if (currentState == GameState.Playing)
            {
                switch (keyData)
                {
                    case Keys.Up: case Keys.W: if (direction.Y == 0) nextDirection = new Point(0, -1); return true;
                    case Keys.Down: case Keys.S: if (direction.Y == 0) nextDirection = new Point(0, 1); return true;
                    case Keys.Left: case Keys.A: if (direction.X == 0) nextDirection = new Point(-1, 0); return true;
                    case Keys.Right: case Keys.D: if (direction.X == 0) nextDirection = new Point(1, 0); return true;

                    // ==========================================
                    // GELİŞTİRİCİ HİLESİ (DEV CHEAT) - P TUŞU
                    // ==========================================
                    case Keys.P:
                        totalGold += (currentActiveLevel * 5); // Bölüm altınını anında hesaba yatır
                        currentState = GameState.LevelComplete; // Bölüm geçme ekranını tetikle
                        this.Invalidate(); // Ekranı anında güncelle
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            if (currentState == GameState.Menu)
            {
                if (menuBg != null) g.DrawImage(menuBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                DrawMenuOverlay(g);
            }
            else if (currentState == GameState.HowToPlay)
            {
                g.Clear(Color.FromArgb(15, 15, 15));
                StringFormat format = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("NASIL OYNANIR?", new Font("Segoe UI", 36, FontStyle.Bold), Brushes.White, new PointF(400, 70), format);
                string text = "- Yılanı WASD veya YÖN TUŞLARI ile yönetin.\n\n" +
                              "- Her yem +10 ALTIN kazandırır.\n\n" +
                              "- HEDEF skora ulaşıp BÖLÜMÜ TEMİZLEYİN.\n\n" +
                              "- Kazandığınız altınlarla MAĞAZADAN Taç veya Kostüm alın!\n\n" +
                              "- Unutmayın, Taç takmak ve Kostüm giymek sadece GÖSTERİŞTİR.\n\n" +
                              "Zindandan Çıkmak İçin [ESC] Basın";
                g.DrawString(text, new Font("Segoe UI", 12), Brushes.LightGray, new PointF(400, 200), format);
            }
            else if (currentState == GameState.LevelSelect)
            {
                if (menuBg != null) g.DrawImage(menuBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), 0, 0, this.Width, this.Height);
                DrawLevelSelectionGrid(g);
            }
            else if (currentState == GameState.Shop)
            {
                if (menuBg != null) g.DrawImage(menuBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 10, 10, 10)), 0, 0, this.Width, this.Height);
                DrawModernShopUI(g);
            }
            else if (currentState == GameState.Playing || currentState == GameState.GameOver || currentState == GameState.LevelComplete)
            {
                g.Clear(zindanColor);

                foreach (Point wall in walls) { Rectangle wRect = new Rectangle(wall.X * CellSize, wall.Y * CellSize, CellSize, CellSize); g.FillRectangle(Brushes.DarkSlateGray, wRect); g.DrawRectangle(Pens.Black, wRect); }

                g.FillEllipse(new SolidBrush(Color.FromArgb(100, yemColor)), food.X * CellSize - 4, food.Y * CellSize - 4, CellSize + 8, CellSize + 8);
                g.FillEllipse(new SolidBrush(yemColor), food.X * CellSize + 4, food.Y * CellSize + 4, CellSize - 8, CellSize - 8);

                if (isEagleActive)
                {
                    Rectangle eRect = new Rectangle(eaglePos.X * CellSize, eaglePos.Y * CellSize, CellSize, CellSize);
                    Point[] eaglePoly = { new Point(eRect.X + CellSize / 2, eRect.Y), new Point(eRect.Right, eRect.Y + CellSize / 2), new Point(eRect.X + CellSize / 2, eRect.Bottom), new Point(eRect.X, eRect.Y + CellSize / 2) };
                    g.FillPolygon(Brushes.SaddleBrown, eaglePoly); g.FillEllipse(Brushes.Black, eRect.X + 8, eRect.Y + 8, 4, 4); g.FillEllipse(Brushes.Black, eRect.X + 13, eRect.Y + 8, 4, 4);
                }

                for (int i = 0; i < snake.Count; i++)
                {
                    Rectangle rect = new Rectangle(snake[i].X * CellSize, snake[i].Y * CellSize, CellSize - 2, CellSize - 2);
                    Brush snakeBrush;
                    if (equipAsilSkin) snakeBrush = (i % 2 == 0) ? Brushes.Black : Brushes.White;
                    else snakeBrush = (i == 0) ? Brushes.LimeGreen : Brushes.ForestGreen;

                    if (equipAsilSkin) { g.FillRectangle(snakeBrush, rect); g.DrawRectangle(Pens.Gray, rect); }
                    else { g.FillPie(snakeBrush, rect.X, rect.Y, rect.Width, rect.Height, 0, 360); }

                    if (i == 0)
                    {
                        int eyeSize = 6, leftX = 0, leftY = 0, rightX = 0, rightY = 0;
                        if (direction.X == 1) { leftX = rect.X + CellSize - 10; leftY = rect.Y + 4; rightX = rect.X + CellSize - 10; rightY = rect.Y + CellSize - 10; }
                        else if (direction.X == -1) { leftX = rect.X + 4; leftY = rect.Y + 4; rightX = rect.X + 4; rightY = rect.Y + CellSize - 10; }
                        else if (direction.Y == 1) { leftX = rect.X + 4; leftY = rect.Y + CellSize - 10; rightX = rect.X + CellSize - 10; rightY = rect.Y + CellSize - 10; }
                        else if (direction.Y == -1) { leftX = rect.X + 4; leftY = rect.Y + 4; rightX = rect.X + CellSize - 10; rightY = rect.Y + 4; }
                        g.FillEllipse(equipAsilSkin ? Brushes.Red : Brushes.White, leftX, leftY, eyeSize, eyeSize); g.FillEllipse(equipAsilSkin ? Brushes.Red : Brushes.White, rightX, rightY, eyeSize, eyeSize);

                        if (equipCrown)
                        {
                            Point[] crownPts = { new Point(rect.X + 2, rect.Y - 8), new Point(rect.X + CellSize - 4, rect.Y - 8), new Point(rect.X + CellSize - 4, rect.Y + 4), new Point(rect.X + CellSize / 2, rect.Y - 2), new Point(rect.X + 2, rect.Y + 4) };
                            g.FillPolygon(Brushes.Gold, crownPts);
                        }
                    }
                }

                DrawTopBarHUD(g);

                if (flashAlpha > 0) g.FillRectangle(new SolidBrush(Color.FromArgb(flashAlpha, 255, 0, 0)), 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                DrawOverlayMessages(g);
            }
        }

        private void DrawMenuOverlay(Graphics g)
        {
            StringFormat format = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString($"TOPLAM ALTIN: {totalGold} G", new Font("Segoe UI", 16, FontStyle.Bold), Brushes.Gold, new PointF(this.ClientSize.Width - 20, 20), format);
            g.DrawString($"AÇILMIŞ BÖLÜM: {unlockedMaxLevel}/70", new Font("Segoe UI", 12), Brushes.LightGray, new PointF(this.ClientSize.Width - 20, 50), format);
        }

        private void DrawLevelSelectionGrid(Graphics g)
        {
            StringFormat format = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString("ZİNDAN HARİTASI", new Font("Segoe UI", 36, FontStyle.Bold), Brushes.White, new PointF(400, 40), format);

            int startX = 50, startY = 120, colSize = 65, rowSize = 50;
            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    int level = (row * 10) + col + 1;
                    Rectangle rect = new Rectangle(startX + col * colSize, startY + row * rowSize, colSize - 5, rowSize - 5);

                    Brush rectBrush; Color textCol;
                    if (level < unlockedMaxLevel) { rectBrush = new SolidBrush(Color.FromArgb(100, Color.LimeGreen)); textCol = Color.White; }
                    else if (level == unlockedMaxLevel) { rectBrush = new SolidBrush(Color.FromArgb(200, Color.Gold)); textCol = Color.Black; }
                    else { rectBrush = new SolidBrush(Color.FromArgb(100, Color.Gray)); textCol = Color.DarkGray; }

                    g.FillRectangle(rectBrush, rect); g.DrawRectangle(new Pen(textCol), rect);
                    g.DrawString(level.ToString(), new Font("Segoe UI Semibold", 10, FontStyle.Bold), new SolidBrush(textCol), new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2 - 5), format);
                }
            }
        }

        private void DrawModernShopUI(Graphics g)
        {
            StringFormat format = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString("ZİNDAN MAĞAZASI", new Font("Segoe UI", 36, FontStyle.Bold), Brushes.Gold, new PointF(400, 30), format);
            g.DrawString($"BAKİYE: {totalGold} G", new Font("Segoe UI", 20, FontStyle.Bold), Brushes.White, new PointF(400, 90), format);

            g.FillRectangle(new SolidBrush(Color.FromArgb(150, 20, 20, 20)), 100, 150, 250, 280);
            g.DrawRectangle(new Pen(Color.Gold, 2), 100, 150, 250, 280);
            g.DrawString("KRAL TACI", new Font("Segoe UI", 16, FontStyle.Bold), Brushes.Gold, new PointF(225, 170), format);
            Point[] crownPts = { new Point(185, 230), new Point(265, 230), new Point(265, 270), new Point(225, 250), new Point(185, 270) };
            g.FillPolygon(Brushes.Gold, crownPts);
            g.DrawString("Yılanın başına takılır.\nSadece gösteriş içindir.", new Font("Segoe UI", 10), Brushes.LightGray, new PointF(225, 300), format);

            g.FillRectangle(new SolidBrush(Color.FromArgb(150, 20, 20, 20)), 450, 150, 250, 280);
            g.DrawRectangle(new Pen(Color.White, 2), 450, 150, 250, 280);
            g.DrawString("ASİL KOSTÜM", new Font("Segoe UI", 16, FontStyle.Bold), Brushes.White, new PointF(575, 170), format);
            g.FillRectangle(Brushes.Black, 540, 230, 35, 35); g.FillRectangle(Brushes.White, 575, 230, 35, 35);
            g.DrawString("Yılanın rengini asil bir\nSiyah/Beyaz temaya sokar.", new Font("Segoe UI", 10), Brushes.LightGray, new PointF(575, 300), format);
        }

        private void DrawTopBarHUD(Graphics g)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), 0, 0, this.ClientSize.Width, 30);
            g.DrawLine(new Pen(Color.FromArgb(100, 255, 255, 255)), 0, 30, this.ClientSize.Width, 30);

            Font topFont = new Font("Segoe UI", 12, FontStyle.Bold);
            string timeString = TimeSpan.FromSeconds(levelTimeInSeconds).ToString(@"mm\:ss");

            g.DrawString($"BÖLÜM: {currentActiveLevel} ({tierName})", topFont, Brushes.White, 10, 4);
            g.DrawString($"HEDEF: {foodsEatenInLevel} / {targetFoodsToClear}", topFont, Brushes.LimeGreen, 250, 4);
            g.DrawString($"ALTIN: {totalGold} G", topFont, Brushes.Gold, 450, 4);
            g.DrawString($"SÜRE: {timeString}", topFont, Brushes.DeepSkyBlue, 680, 4);
        }

        private void DrawOverlayMessages(Graphics g)
        {
            StringFormat format = new StringFormat { Alignment = StringAlignment.Center };
            if (currentState == GameState.LevelComplete)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), 0, 0, this.Width, this.Height);
                g.DrawString($"ZİNDAN ODA {currentActiveLevel}", new Font("Segoe UI", 36, FontStyle.Bold), Brushes.LimeGreen, new PointF(400, 200), format);
                g.DrawString("TEMİZLENDİ!", new Font("Segoe UI", 24, FontStyle.Bold), Brushes.White, new PointF(400, 260), format);
                g.DrawString($"Bölüm Sonu Bonusu: +{currentActiveLevel * 5} Altın", new Font("Segoe UI", 16), Brushes.Gold, new PointF(400, 310), format);
                if (currentActiveLevel >= 70) g.DrawString("ZİNDANI TAMAMEN FETHETTİN!", new Font("Segoe UI", 16, FontStyle.Bold), Brushes.DeepSkyBlue, new PointF(400, 360), format);
            }
            if (currentState == GameState.GameOver)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(220, 0, 0, 0)), 0, 0, this.Width, this.Height);
                g.DrawString("ZİNDANDA ÖLDÜN!", new Font("Segoe UI", 48, FontStyle.Bold), Brushes.Red, new PointF(400, 200), format);
                g.DrawString($"Ulaşılan Bölüm: {currentActiveLevel} - Skor: {foodsEatenInLevel} - Toplam Altın: {totalGold}", new Font("Segoe UI", 18), Brushes.White, new PointF(400, 290), format);
                g.DrawString("Haritaya dönmek için [ESC] Basın", new Font("Segoe UI", 16), Brushes.LightGray, new PointF(400, 360), format);
            }
        }

        private void EndGameDungeon() { gameTimer.Stop(); currentState = GameState.GameOver; ManageUIState(GameState.GameOver); SaveGameProgress(); }
    }
}