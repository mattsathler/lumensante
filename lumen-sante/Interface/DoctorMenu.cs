using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using lumen_sante.Gameplay;
using static lumen_sante.Gameplay.DoctorProfiles;
using Badge = RAGENativeUI.Elements.UIMenuItem.BadgeStyle;
using Keys = System.Windows.Forms.Keys;

namespace lumen_sante.Interface
{
    internal sealed class DoctorMenu
    {
        private sealed class ProfileMenu : UIMenu
        {
            private const int PauseControl = 199;
            private const int PauseAlternateControl = 200;
            private (int Selected, int First)? pendingPosition;
            private bool escapeWasDown;
            private bool escapeHandled;
            private bool suppressBack;
            internal Action Back { get; set; }
            internal Action CloseRequested { get; set; }

            internal ProfileMenu() : base("Lumen Santé", "Médicos") { }

            public override void GoBack()
            {
                if (!suppressBack) (Back ?? CloseRequested)?.Invoke();
            }

            internal void ProcessCloseShortcut()
            {
                bool escapeDown = Game.IsKeyDown(Keys.Escape);
                bool pressed = Visible && !Game.Console.IsOpen && escapeDown && !escapeWasDown;
                escapeWasDown = escapeDown;
                escapeHandled |= pressed;
                suppressBack = escapeHandled;
                if (!Visible && !escapeHandled) return;
                if (!escapeDown) escapeHandled = false;
                if (Game.Console.IsOpen) return;

                NativeFunction.Natives.DISABLE_CONTROL_ACTION(0, PauseControl, true);
                NativeFunction.Natives.DISABLE_CONTROL_ACTION(0, PauseAlternateControl, true);
                NativeFunction.Natives.DISABLE_CONTROL_ACTION(2, PauseControl, true);
                NativeFunction.Natives.DISABLE_CONTROL_ACTION(2, PauseAlternateControl, true);
                if (pressed) CloseRequested?.Invoke();
            }

            internal void RestorePosition((int Selected, int First)? position) => pendingPosition = position;

            internal void ClearPage()
            {
                if (HoveredItem >= 0 && HoveredItem < MenuItems.Count)
                    MenuItems[HoveredItem].Hovered = false;
                HoveredItem = -1;
                Clear();
            }

            private void ApplyPosition()
            {
                var position = pendingPosition;
                pendingPosition = null;
                if (!position.HasValue || MenuItems.Count == 0) return;
                int selected = Math.Min(MenuItems.Count - 1, Math.Max(0, position.Value.Selected));
                CurrentSelection = selected;
                int count = Math.Min(MaxItemsOnScreen, MenuItems.Count);
                int first = Math.Min(MenuItems.Count - count, Math.Max(0, position.Value.First));
                first = Math.Min(selected, Math.Max(first, selected - count + 1));
                MinItem = first;
                MaxItem = first + count - 1;
            }

            public override void ProcessControl()
            {
                ApplyPosition();
                base.ProcessControl();
                ApplyPosition();
            }

            public override void ProcessMouse()
            {
                base.ProcessMouse();
                ApplyPosition();
            }
        }

        private static readonly string[] FeatureNames =
        {
            "Largura do nariz", "Altura da ponta do nariz", "Comprimento do nariz",
            "Altura do dorso do nariz", "Queda da ponta do nariz", "Desvio do nariz",
            "Altura das sobrancelhas", "Profundidade das sobrancelhas", "Altura das maçãs do rosto",
            "Largura das maçãs do rosto", "Largura das bochechas", "Abertura dos olhos",
            "Espessura dos lábios", "Largura da mandíbula", "Formato da mandíbula",
            "Altura do queixo", "Comprimento do queixo", "Largura do queixo",
            "Covinha do queixo", "Largura do pescoço"
        };
        private static readonly string[] FeaturePairs =
        {
            "Nariz · largura e altura", "Nariz · comprimento e dorso", "Nariz · ponta e desvio",
            "Sobrancelhas", "Maçãs do rosto", "Bochechas e olhos", "Lábios e mandíbula",
            "Mandíbula e queixo", "Queixo · comprimento e largura", "Queixo e pescoço"
        };
        private static readonly string[] OverlayNames =
        {
            "Imperfeições", "Barba", "Sobrancelhas", "Envelhecimento", "Maquiagem", "Blush",
            "Textura da pele", "Danos do sol", "Batom", "Sardas e pintas", "Pelos no peito",
            "Marcas no corpo", "Marcas adicionais"
        };
        private static readonly string[] TattooRegions =
        {
            "Todas", "Tronco", "Cabeça", "Braço esquerdo", "Braço direito",
            "Perna esquerda", "Perna direita", "Outras"
        };
        private static readonly string[] ClothingNames =
        {
            "", "Máscara", "", "Braços e luvas", "Calças", "Bolsas", "Calçados",
            "Acessórios", "Camiseta interna", "Colete", "Adesivos", "Parte de cima"
        };
        private static readonly string[] PropNames =
        {
            "Chapéus", "Óculos", "Brincos", "Acessórios de boca", "Mão esquerda", "Mão direita", "Relógios", "Pulseiras",
            "Quadril", "Pé esquerdo", "Pé direito", "Mão esquerda (físico)", "Mão direita (físico)"
        };
        private readonly DoctorProfiles profiles;
        private readonly ProfileMenu menu;
        private readonly Func<bool> onDuty;
        private readonly Func<Guid, bool> archive;
        private readonly Random random = new Random();
        private Guid activeId;
        private Guid selectedOutfitId;
        private Ped activePed;
        private Guid wornOutfitId;
        private Outfit wornAppearance;
        private Ped previewPed;
        private Outfit previousOutfit;
        private Model? originalModel;
        private Outfit originalOutfit;
        private Profile draft;
        private bool draftDefault;
        private Outfit outfitDraft;
        private int featureCamera;
        private bool clonePreview;
        private long retryProfileAt;
        private Action endPreview;
        private readonly Dictionary<string, (int Selected, int First)> pagePositions =
            new Dictionary<string, (int Selected, int First)>();
        private string pageKey;
        private string previewBaseline;
        private bool confirmExitOpen;
        private int previewAngle;
        private int tattooRegion;
        private bool appliedTattoosOnly;

        internal DoctorMenu(MenuPool pool, DoctorProfiles profiles, Func<bool> onDuty,
            Func<Guid, bool> archive)
        {
            this.profiles = profiles;
            this.onDuty = onDuty;
            this.archive = archive;
            menu = new ProfileMenu { CloseRequested = RequestClose };
            menu.OnMenuClose += sender => CancelPreview();
            pool.Add(menu);
        }

        internal bool IsOpen => menu.Visible;
        internal Profile Active => profiles.Items.FirstOrDefault(item => item.Id == activeId);
        internal Guid ActiveId => activeId;
        internal bool CanStartShift(Ped player) => player != null && profiles.Default != null && Active != null &&
            activePed != null && activePed == player && player.Exists() && player.Model.Hash == DoctorAppearance.ModelFor(Active).Hash;

        internal bool RestoreProfile(Ped player)
        {
            if (IsOpen || onDuty() || player == null || !player.Exists() || !player.IsAlive || !player.IsOnFoot) return false;
            Profile profile = Active ?? profiles.Default;
            if (profile == null || (activePed != null && activePed == player) ||
                GameClock.NowMilliseconds < retryProfileAt) return false;
            retryProfileAt = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(3000);
            return Activate(profile, activeId == Guid.Empty);
        }

        internal void ApplyShiftOutfit(bool starting)
        {
            Profile profile = Active;
            Ped player = Game.LocalPlayer.Character;
            if (profile == null || player == null || !player.Exists() || player != activePed) return;
            Outfit[] choices = profile.Outfits.Where(item => item.Duty == starting).ToArray();
            if (choices.Length == 0) return;
            Outfit selected = starting ? choices[random.Next(choices.Length)] :
                choices.FirstOrDefault(item => item.Id == profile.LastCasual) ?? choices[0];
            WearAndRemember(profile, selected, player);
        }

        internal void Toggle()
        {
            if (menu.Visible)
            {
                RequestClose();
                return;
            }
            pagePositions.Clear();
            pageKey = null;
            selectedOutfitId = Guid.Empty;
            ShowHome();
            menu.Visible = true;
        }

        internal void ProcessCloseShortcut() => menu.ProcessCloseShortcut();

        private void RequestClose()
        {
            if (!confirmExitOpen) RequestPreviewExit(Close);
        }

        internal void Close()
        {
            CancelPreview();
            menu.Visible = false;
        }

        private bool Activate(Profile profile, bool randomCasual)
        {
            if (onDuty()) return false;
            Ped before = Game.LocalPlayer.Character;
            if (before == null || !before.Exists() || !before.IsAlive || !before.IsOnFoot) return false;
            if (!originalModel.HasValue)
            {
                originalModel = before.Model;
                originalOutfit = DoctorAppearance.CaptureOutfit(before);
            }
            Ped player = DoctorAppearance.ChangeModel(DoctorAppearance.ModelFor(profile));
            if (player == null) return false;
            activeId = profile.Id;
            activePed = player;
            selectedOutfitId = Guid.Empty;
            wornOutfitId = Guid.Empty;
            wornAppearance = null;
            if (before != player) NativeFunction.Natives.SET_PED_DEFAULT_COMPONENT_VARIATION(player);
            DoctorAppearance.Apply(player, profile.Face);
            Outfit[] casual = profile.Outfits.Where(item => !item.Duty).ToArray();
            Outfit selected = casual.Length == 0 ? null : randomCasual ? casual[random.Next(casual.Length)] :
                casual.FirstOrDefault(item => item.Id == profile.LastCasual) ?? casual[0];
            if (selected != null) WearAndRemember(profile, selected, player);
            return true;
        }

        private void WearAndRemember(Profile profile, Outfit outfit, Ped player)
        {
            DoctorAppearance.Wear(player, outfit);
            wornOutfitId = outfit.Id;
            wornAppearance = DoctorAppearance.CaptureOutfit(player);
            if (!outfit.Duty && !profiles.RememberCasual(profile.Id, outfit.Id)) SaveFailed();
        }

        private void ShowHome()
        {
            Page("Menu principal");
            Row("Médicos", ShowDoctors);
            bool wardrobeAvailable = profiles.Default != null && Active != null;
            Row("Armário", OpenWardrobe, wardrobeAvailable, right: wardrobeAvailable ? Badge.None : Badge.Lock);
        }

        private void ShowDoctors()
        {
            Page("Médicos", ShowHome);
            if (!profiles.Available)
            {
                Row("Perfis indisponíveis", null, right: Badge.Alert);
                return;
            }
            Row("Criar médico", ShowCreate, !onDuty(), right: onDuty() ? Badge.Lock : Badge.None, tone: ActionTone.Accent);
            foreach (Profile profile in profiles.Items)
            {
                bool active = profile.Id == activeId;
                bool canSelect = active || !onDuty();
                Row(profile.Name, () =>
                {
                    if (!active && !Activate(profile, false))
                        Game.DisplayNotification("Fique a pé e fora do plantão para trocar de médico.");
                    ShowDoctors();
                }, canSelect, right: canSelect ? Badge.None : Badge.Lock, rightLabel: active ? "Ativo" : null);
            }
            AddDoctorActions(Active);
        }

        private void ShowCreate()
        {
            if (onDuty()) return;
            Page("Novo médico", ShowDoctors);
            Row("Modelo masculino", () => Create(false));
            Row("Modelo feminino", () => Create(true));
        }

        private void Create(bool female)
        {
            if (onDuty()) return;
            string name = Prompt("");
            if (name != null) StartFacePreview(DoctorProfiles.Create(name, female), ShowCreate, true);
        }

        private void AddDoctorActions(Profile profile)
        {
            Guid id = profile?.Id ?? Guid.Empty;
            bool canChange = profile != null && !onDuty();
            Row("Editar médico", () => StartFacePreview(profiles.Copy(profile), ShowDoctors), canChange);
            Row("Excluir médico", () => Confirm("Excluir " + profile.Name + "?", () =>
            {
                if (onDuty() || !archive(id)) { SaveFailed(); return; }
                if (activeId == id)
                {
                    activeId = Guid.Empty;
                    activePed = null;
                    if (profiles.Default != null) Activate(profiles.Default, false);
                    else RestoreOriginal();
                }
                ShowDoctors();
            }, ShowDoctors), canChange, tone: ActionTone.Destructive);
        }

        private void RestoreOriginal()
        {
            if (!originalModel.HasValue) return;
            Ped player = DoctorAppearance.ChangeModel(originalModel.Value);
            if (player != null && originalOutfit != null) DoctorAppearance.Wear(player, originalOutfit, true);
        }

        private void StartFacePreview(Profile profile, Action back, bool isNew = false)
        {
            if (onDuty() || !BeginPreview()) return;
            draft = profile;
            draftDefault = profiles.DefaultId == profile.Id;
            previewBaseline = isNew ? null : FaceEditStamp();
            endPreview = back;
            Ped player = previewPed;
            clonePreview = true;
            previewPed = null;
            try
            {
                Vector3 position = NativeFunction.Natives.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS<Vector3>(player, 3f, 0f, 0f);
                previewPed = new Ped(DoctorAppearance.ModelFor(profile), position, player.Heading);
                if (!previewPed.Exists()) throw new InvalidOperationException("Modelo indisponível.");
                previewPed.IsPersistent = true;
                previewPed.IsPositionFrozen = true;
                previewPed.BlockPermanentEvents = true;
                NativeFunction.Natives.SET_ENTITY_COLLISION(previewPed, false, false);
                NativeFunction.Natives.SET_ENTITY_INVINCIBLE(previewPed, true);
                NativeFunction.Natives.SET_PED_CAN_PLAY_AMBIENT_ANIMS(previewPed, false);
                NativeFunction.Natives.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS(previewPed, false);
            }
            catch (InvalidOperationException error)
            {
                CancelPreview();
                Game.LogTrivial("[Lumen Santé] Falha ao preparar prévia do médico: " + error.Message);
                Game.DisplayNotification("Não foi possível abrir a prévia do médico.");
                return;
            }
            Outfit display = profile.Id == activeId ? previousOutfit : profile.Outfits.FirstOrDefault(item => !item.Duty);
            if (display != null) DoctorAppearance.Wear(previewPed, display);
            DoctorAppearance.Apply(previewPed, draft.Face);
            ShowFace();
        }

        private void ShowFace()
        {
            StartCamera(false);
            Page("Editar médico", ReturnFromPreview);
            Row("Nome · " + draft.Name, () =>
            {
                string name = Prompt(draft.Name);
                if (name != null) draft.Name = name;
                ShowFace();
            });
            Row("Ascendência", ShowParents);
            Row("Traços faciais", ShowFeatures);
            Row("Aparência", ShowAppearance);
            var standard = MenuPalette.Apply(new UIMenuCheckboxItem("Personagem padrão", draftDefault));
            standard.CheckboxEvent += (item, value) => draftDefault = value;
            menu.AddItem(standard);
            Row("Salvar médico", () =>
            {
                if (!CommitFaceEdit()) return;
                CancelPreview();
                ShowDoctors();
            }, tone: ActionTone.Accent);
        }

        private void ShowAppearance()
        {
            StartCamera(false);
            Page("Aparência", ShowFace);
            Row("Cabelo e olhos", ShowHair, right: Badge.Barber);
            Row("Pele e maquiagem", ShowOverlays, right: Badge.Makeup);
            Row("Tatuagens", ShowTattoos, right: Badge.Tatoo);
        }

        private string FaceEditStamp() => profiles.EditStamp(draft) + "/" + draftDefault;

        private bool CommitFaceEdit()
        {
            if (onDuty() || !profiles.SaveProfile(draft, draftDefault)) { SaveFailed(); return false; }
            if (draft.Id == activeId && activePed != null && activePed.Exists())
                DoctorAppearance.Apply(activePed, draft.Face);
            else if (draftDefault && Active == null && !Activate(draft, false))
                Game.DisplayNotification("O médico padrão será aplicado quando o personagem estiver disponível.");
            return true;
        }

        private void ShowParents()
        {
            StartCamera();
            Page("Ascendência", ShowFace);
            Appearance face = draft.Face;
            UIMenuNumericScrollerItem<int> texture = null;
            bool updating = false;
            var head = Number("Modelo do rosto", 0, DoctorAppearance.Pieces(previewPed, 0, false) - 1, face.Head, value =>
            {
                face.Head = value;
                face.HeadTexture = 0;
                updating = true;
                ResetTextureRange(texture, TextureMaximum(0, value, false));
                updating = false;
                ApplyFace();
            });
            head.Formatter = value => DoctorAppearance.VariationName(previewPed, 0, value, 0, false) ?? value.ToString();
            texture = Number("Textura do rosto", 0, TextureMaximum(0, face.Head, false), face.HeadTexture, value =>
            {
                if (updating) return;
                face.HeadTexture = value;
                ApplyFace();
            });
            texture.Formatter = value => DoctorAppearance.VariationName(previewPed, 0, face.Head, value, false) ?? value.ToString();
            Number("Paleta do rosto", 0, 3, face.HeadPalette, value => { face.HeadPalette = value; ApplyFace(); });
            int[] parents = DoctorAppearance.ParentIds();
            for (int index = 0; index < 3; index++)
            {
                int slot = index;
                Choice("Rosto " + (slot + 1), parents, draft.Face.Shape[slot], v => { draft.Face.Shape[slot] = v; ApplyFace(); });
                Choice("Tom de pele " + (slot + 1), parents, draft.Face.Skin[slot], v => { draft.Face.Skin[slot] = v; ApplyFace(); });
            }
            Bar("Mistura do rosto", 0, 100, draft.Face.Mix[0], value => SetMix(0, value));
            Bar("Mistura da pele", 0, 100, draft.Face.Mix[1], value => SetMix(1, value));
            Bar("Mistura do terceiro rosto", 0, 100, draft.Face.Mix[2], value => SetMix(2, value));
        }

        private void SetMix(int index, int value) { draft.Face.Mix[index] = value; ApplyFace(); }

        private void ShowFeatures()
        {
            StartCamera();
            Page("Traços faciais", ShowFace);
            for (int index = 0; index < FeaturePairs.Length; index++)
            {
                int pair = index;
                Row(FeaturePairs[pair], () => ShowFeaturePair(pair));
            }
        }

        private void ShowFeaturePair(int pair)
        {
            Page(FeaturePairs[pair], ShowFeatures);
            int horizontal = pair * 2;
            int vertical = horizontal + 1;
            int[] features = draft.Face.Features;
            bool syncing = false;
            var grid = new UIMenuGridPanel
            {
                LeftLabel = "−", RightLabel = "+", TopLabel = "+", BottomLabel = "−",
                DotColor = MenuPalette.Accent,
                Value = GridPosition()
            };
            var x = Bar(FeatureNames[horizontal], -100, 100, features[horizontal], value => MoveBar(horizontal, value));
            var y = Bar(FeatureNames[vertical], -100, 100, features[vertical], value => MoveBar(vertical, value));
            x.Panels.Add(grid);
            y.Panels.Add(grid);
            grid.ValueChanged += (sender, previous, position) =>
            {
                if (syncing) return;
                syncing = true;
                x.Value = (int)Math.Round(position.X * 200f - 100f);
                y.Value = (int)Math.Round(100f - position.Y * 200f);
                SetFeature(horizontal, x.Value);
                SetFeature(vertical, y.Value);
                syncing = false;
            };

            Vector2 GridPosition() => new Vector2((features[horizontal] + 100) / 200f, (100 - features[vertical]) / 200f);
            void MoveBar(int feature, int value)
            {
                if (syncing) return;
                syncing = true;
                SetFeature(feature, value);
                grid.Value = GridPosition();
                syncing = false;
            }
            void SetFeature(int feature, int value)
            {
                if (features[feature] == value) return;
                features[feature] = value;
                DoctorAppearance.SetFaceFeature(previewPed, feature, value);
            }
        }

        private void ShowHair()
        {
            StartCamera();
            Page("Cabelo e olhos", ShowAppearance);
            Appearance face = draft.Face;
            UIMenuNumericScrollerItem<int> texture = null;
            bool updating = false;
            int[] styles = DoctorAppearance.HairStyles(previewPed);
            if (styles.Length > 0) Choice("Penteado", styles, face.Hair, value =>
            {
                face.Hair = value;
                face.HairTexture = 0;
                updating = true;
                ResetTextureRange(texture, TextureMaximum(2, value, false));
                updating = false;
                DoctorAppearance.ApplyHair(previewPed, face);
            }, value => DoctorAppearance.VariationName(previewPed, 2, value, 0, false));
            texture = Number("Textura", 0, TextureMaximum(2, face.Hair, false), face.HairTexture, value =>
            {
                if (updating) return;
                face.HairTexture = value;
                DoctorAppearance.ApplyHair(previewPed, face);
            });
            texture.Formatter = value => DoctorAppearance.VariationName(previewPed, 2, face.Hair, value, false) ?? value.ToString();
            Number("Cor do cabelo", 0, DoctorAppearance.HairColors - 1, face.HairColor,
                value => { face.HairColor = value; DoctorAppearance.ApplyHair(previewPed, face); });
            Number("Mechas", 0, DoctorAppearance.HairColors - 1, face.Highlights,
                value => { face.Highlights = value; DoctorAppearance.ApplyHair(previewPed, face); });
            Number("Paleta do penteado", 0, 3, face.HairPalette,
                value => { face.HairPalette = value; DoctorAppearance.ApplyHair(previewPed, face); });
            var eyes = Number("Cor dos olhos", 0, 31, face.Eyes,
                value => { face.Eyes = value; DoctorAppearance.SetEyeColor(previewPed, value); });
            eyes.Formatter = DoctorAppearance.EyeColorName;
        }

        private void ShowOverlays()
        {
            StartCamera();
            Page("Pele e maquiagem", ShowAppearance);
            for (int index = 0; index < OverlayNames.Length; index++)
            {
                int slot = index;
                Row(OverlayNames[slot], () => ShowOverlay(slot));
            }
        }

        private void ShowOverlay(int slot)
        {
            Page(OverlayNames[slot], ShowOverlays);
            Appearance face = draft.Face;
            void Set(int[] values, int value)
            {
                values[slot] = value;
                DoctorAppearance.ApplyOverlay(previewPed, face, slot);
            }
            var overlay = Number("Variação", -1, DoctorAppearance.OverlayCount(slot) - 1, face.Overlays[slot],
                value => Set(face.Overlays, value));
            overlay.Formatter = value => value < 0 ? "Nenhuma" : value.ToString();
            Bar("Opacidade", 0, 100, face.Opacity[slot], value => Set(face.Opacity, value));
            int type = DoctorAppearance.OverlayColorType(slot);
            if (type != 0)
            {
                int max = (type == 1 ? DoctorAppearance.HairColors : DoctorAppearance.MakeupColors) - 1;
                Number("Cor", 0, max, face.Color[slot], value => Set(face.Color, value));
                Number("Cor secundária", 0, max, face.SecondaryColor[slot], value => Set(face.SecondaryColor, value));
            }
        }

        private void ShowTattoos()
        {
            StartCamera(false);
            var catalog = DoctorAppearance.Tattoos(draft.Female);
            Page("Tatuagens", ShowAppearance);
            AddPreviewAngle();
            var region = Number("Região do corpo", 0, TattooRegions.Length - 1, tattooRegion, value =>
            {
                tattooRegion = value;
                ShowTattoos();
            });
            region.Formatter = value => TattooRegions[value];
            var applied = MenuPalette.Apply(new UIMenuCheckboxItem("Somente aplicadas", appliedTattoosOnly));
            applied.CheckboxEvent += (sender, enabled) =>
            {
                appliedTattoosOnly = enabled;
                ShowTattoos();
            };
            menu.AddItem(applied);

            var currentTattoos = new HashSet<(uint Collection, uint Preset)>(
                draft.Face.Tattoos.Select(tattoo => (tattoo.Collection, tattoo.Preset)));
            var visible = catalog.Where(option => (tattooRegion == 0 || option.Zone == tattooRegion - 1) &&
                (!appliedTattoosOnly || currentTattoos.Contains((option.Tattoo.Collection, option.Tattoo.Preset)))).ToArray();
            menu.AddItems(visible.Select(option =>
            {
                bool Matches(Decoration tattoo) => tattoo.Collection == option.Tattoo.Collection && tattoo.Preset == option.Tattoo.Preset;
                var item = MenuPalette.Apply(new UIMenuCheckboxItem(option.Name, draft.Face.Tattoos.Any(Matches)));
                item.CheckboxEvent += (sender, enabled) =>
                {
                    draft.Face.Tattoos.RemoveAll(tattoo => Matches(tattoo));
                    if (enabled) draft.Face.Tattoos.Add(option.Tattoo);
                    DoctorAppearance.ApplyTattoos(previewPed, draft.Face);
                    if (appliedTattoosOnly && !enabled) ShowTattoos();
                };
                return item;
            }));
            if (visible.Length == 0) Row("Nenhuma tatuagem para este filtro", null);
        }

        private void ShowWardrobe()
        {
            Profile profile = Active;
            if (profiles.Default == null || profile == null) { ShowHome(); return; }
            Page("Armário · " + profile.Name, ShowHome, "armário/" + profile.Id);
            bool canCreate = profile.Outfits.Count < OutfitLimit;
            Row("Criar outfit", () => EditOutfit(null), canCreate, right: canCreate ? Badge.None : Badge.Lock,
                tone: ActionTone.Accent);
            Guid worn = GetWornOutfit();
            Outfit selected = null;
            foreach (Outfit outfit in profile.Outfits)
            {
                bool isSelected = outfit.Id == selectedOutfitId;
                if (isSelected) selected = outfit;
                Row(outfit.Name + (outfit.Duty ? " · Plantão" : ""), () =>
                {
                    selectedOutfitId = outfit.Id;
                    Ped player = Game.LocalPlayer.Character;
                    if (outfit.Id != worn && player != null && player == activePed && player.Exists() && player.IsAlive)
                        WearAndRemember(profile, outfit, player);
                    ShowWardrobe();
                }, right: outfit.Id == worn ? Badge.Clothes : isSelected ? Badge.Tick : Badge.None);
            }
            Row("Editar roupa", () => EditOutfit(selected), selected != null);
            Row("Excluir roupa", () => DeleteOutfit(selected), selected != null, tone: ActionTone.Destructive);
        }

        private void OpenWardrobe()
        {
            if (selectedOutfitId == Guid.Empty) selectedOutfitId = GetWornOutfit();
            ShowWardrobe();
        }

        private Guid GetWornOutfit()
        {
            Ped player = Game.LocalPlayer.Character;
            return wornAppearance != null && player != null && player == activePed && player.Exists() &&
                DoctorAppearance.SameClothing(wornAppearance, DoctorAppearance.CaptureOutfit(player))
                ? wornOutfitId : Guid.Empty;
        }

        private void DeleteOutfit(Outfit outfit)
        {
            if (outfit == null) { ShowWardrobe(); return; }
            Confirm("Excluir " + outfit.Name + "?", () =>
            {
                Profile changed = profiles.Copy(Active);
                changed.Outfits.RemoveAll(item => item.Id == outfit.Id);
                if (changed.LastCasual == outfit.Id) changed.LastCasual = Guid.Empty;
                if (!profiles.Put(changed)) { SaveFailed(); return; }
                selectedOutfitId = Guid.Empty;
                if (wornOutfitId == outfit.Id)
                {
                    wornOutfitId = Guid.Empty;
                    wornAppearance = null;
                }
                ShowWardrobe();
            }, ShowWardrobe);
        }

        private void EditOutfit(Outfit outfit)
        {
            if (Active == null || activePed != Game.LocalPlayer.Character || !BeginPreview()) return;
            previewPed = activePed;
            endPreview = ShowWardrobe;
            if (outfit == null)
            {
                string name = Prompt("");
                if (name == null) { CancelPreview(); return; }
                outfitDraft = DoctorAppearance.CaptureOutfit(previewPed);
                outfitDraft.Name = name;
            }
            else outfitDraft = profiles.Copy(outfit);
            previewBaseline = outfit == null ? null : profiles.EditStamp(outfit);
            DoctorAppearance.Wear(previewPed, outfitDraft);
            ShowOutfitEditor();
        }

        private void ShowOutfitEditor()
        {
            StartCamera(false);
            Page("Editar outfit", ReturnFromPreview);
            AddPreviewAngle();
            Row("Nome · " + outfitDraft.Name, () =>
            {
                string name = Prompt(outfitDraft.Name);
                if (name != null) outfitDraft.Name = name;
                ShowOutfitEditor();
            });
            var duty = MenuPalette.Apply(new UIMenuCheckboxItem("Roupa de Plantão", outfitDraft.Duty));
            duty.CheckboxEvent += (item, value) => outfitDraft.Duty = value;
            menu.AddItem(duty);
            for (int index = 0; index < ClothingNames.Length; index++)
            {
                int slot = index;
                if (ClothingNames[slot].Length > 0 && DoctorAppearance.Pieces(previewPed, slot, false) > 0)
                    Row(ClothingNames[slot], () => ShowClothingSlot(slot, false));
            }
            for (int index = 0; index < PropNames.Length; index++)
            {
                int slot = index;
                if (DoctorAppearance.Pieces(previewPed, slot, true) > 0)
                    Row(PropNames[slot], () => ShowClothingSlot(slot, true));
            }
            Row("Salvar outfit", SaveOutfit, tone: ActionTone.Accent);
        }

        private void ShowClothingSlot(int slot, bool prop)
        {
            Page(prop ? PropNames[slot] : ClothingNames[slot], ShowOutfitEditor);
            AddPreviewAngle();
            int[] pieces = prop ? outfitDraft.Props : outfitDraft.Pieces;
            int[] textures = prop ? outfitDraft.PropTextures : outfitDraft.Textures;
            UIMenuNumericScrollerItem<int> texture = null;
            bool updating = false;
            void ApplySlot()
            {
                if (prop) DoctorAppearance.SetProp(previewPed, slot, pieces[slot], textures[slot]);
                else DoctorAppearance.SetPiece(previewPed, slot, pieces[slot], textures[slot], outfitDraft.Palettes[slot]);
            }
            var piece = Number("Peça", prop ? -1 : 0, DoctorAppearance.Pieces(previewPed, slot, prop) - 1,
                pieces[slot], value =>
                {
                    pieces[slot] = value;
                    textures[slot] = 0;
                    updating = true;
                    ResetTextureRange(texture, TextureMaximum(slot, value, prop));
                    updating = false;
                    ApplySlot();
                });
            piece.Formatter = value => value < 0 ? "Sem acessório" :
                DoctorAppearance.VariationName(previewPed, slot, value, 0, prop) ?? value.ToString();
            texture = Number("Textura", 0, TextureMaximum(slot, pieces[slot], prop), textures[slot], value =>
            {
                if (updating) return;
                textures[slot] = value;
                ApplySlot();
            });
            texture.Formatter = value => DoctorAppearance.VariationName(previewPed, slot, pieces[slot], value, prop) ?? value.ToString();
            if (!prop) Number("Paleta", 0, 3, outfitDraft.Palettes[slot],
                value => { outfitDraft.Palettes[slot] = value; ApplySlot(); });
        }

        private int TextureMaximum(int slot, int piece, bool prop) =>
            Math.Max(0, DoctorAppearance.Textures(previewPed, slot, piece, prop) - 1);

        private static void ResetTextureRange(UIMenuNumericScrollerItem<int> item, int last)
        {
            item.Value = 0;
            item.Maximum = Math.Max(1, last);
            item.Enabled = last > 0;
        }

        private void SaveOutfit()
        {
            if (!CommitOutfitEdit()) return;
            CancelPreview();
            ShowWardrobe();
        }

        private bool CommitOutfitEdit()
        {
            Profile profile = profiles.Copy(Active);
            int index = profile.Outfits.FindIndex(item => item.Id == outfitDraft.Id);
            bool changesWornOutfit = outfitDraft.Id == wornOutfitId && index >= 0 &&
                !DoctorAppearance.SameClothing(profile.Outfits[index], outfitDraft);
            if (index < 0) profile.Outfits.Add(outfitDraft);
            else profile.Outfits[index] = outfitDraft;
            if (outfitDraft.Duty && profile.LastCasual == outfitDraft.Id) profile.LastCasual = Guid.Empty;
            if (!profiles.Put(profile)) { SaveFailed(); return false; }
            selectedOutfitId = outfitDraft.Id;
            if (changesWornOutfit)
            {
                wornOutfitId = Guid.Empty;
                wornAppearance = null;
            }
            return true;
        }

        private bool BeginPreview()
        {
            Ped player = Game.LocalPlayer.Character;
            if (player == null || !player.Exists() || !player.IsAlive || !player.IsOnFoot)
            {
                Game.DisplayNotification("Fique a pé para editar o personagem.");
                return false;
            }
            previousOutfit = DoctorAppearance.CaptureOutfit(player);
            previewPed = player;
            previewAngle = 0;
            tattooRegion = 0;
            appliedTattoosOnly = false;
            return true;
        }

        private void CancelPreview()
        {
            StopCamera();
            if (clonePreview)
            {
                if (previewPed != null && previewPed.Exists()) previewPed.Delete();
            }
            else if (previewPed != null && previewPed.Exists() && previewPed.IsAlive && Game.LocalPlayer.Character == previewPed)
            {
                DoctorAppearance.Wear(previewPed, previousOutfit);
            }
            clonePreview = false;
            previewPed = null;
            previousOutfit = null;
            draft = null;
            outfitDraft = null;
            endPreview = null;
            previewBaseline = null;
            confirmExitOpen = false;
        }

        private void ReturnFromPreview()
        {
            Action back = endPreview;
            RequestPreviewExit(() =>
            {
                CancelPreview();
                back?.Invoke();
            });
        }

        private void RequestPreviewExit(Action leave)
        {
            string updated = draft != null ? FaceEditStamp() :
                outfitDraft != null ? profiles.EditStamp(outfitDraft) : null;
            if (updated == null || updated == previewBaseline)
            {
                leave();
                return;
            }

            var items = menu.MenuItems.ToArray();
            string title = menu.SubtitleText;
            string key = pageKey;
            Action back = menu.Back;
            void Resume()
            {
                confirmExitOpen = false;
                Page(title, back, key);
                foreach (UIMenuItem item in items) item.Selected = false;
                menu.AddItems(items);
            }

            Page("Alterações sem salvar", Resume);
            confirmExitOpen = true;
            Row("Continuar editando", Resume);
            Row("Salvar", () =>
            {
                if (draft != null ? CommitFaceEdit() : CommitOutfitEdit()) leave();
            }, tone: ActionTone.Accent);
            Row("Descartar", leave, tone: ActionTone.Destructive);
        }

        private void ApplyFace() => DoctorAppearance.Apply(previewPed, draft.Face);

        private void StartCamera(bool closeUp = true)
        {
            if (previewPed == null || !previewPed.Exists()) return;
            if (featureCamera == 0) featureCamera = NativeFunction.Natives.CREATE_CAM<int>("DEFAULT_SCRIPTED_CAMERA", true);
            if (featureCamera == 0) return;
            Vector3 forward = NativeFunction.Natives.GET_ENTITY_FORWARD_VECTOR<Vector3>(previewPed);
            Vector3 target = NativeFunction.Natives.GET_PED_BONE_COORDS<Vector3>(
                previewPed, closeUp ? 31086 : 11816, 0f, 0f, 0f);
            float distance = closeUp ? 0.85f : 2.7f;
            double radians = closeUp ? 0 : previewAngle * Math.PI / 180;
            float x = (float)(forward.X * Math.Cos(radians) - forward.Y * Math.Sin(radians));
            float y = (float)(forward.X * Math.Sin(radians) + forward.Y * Math.Cos(radians));
            Vector3 position = new Vector3(target.X + x * distance, target.Y + y * distance,
                target.Z + (closeUp ? 0f : 0.15f));
            NativeFunction.Natives.SET_CAM_COORD(featureCamera, position.X, position.Y, position.Z);
            NativeFunction.Natives.POINT_CAM_AT_COORD(featureCamera, target.X, target.Y, target.Z);
            if (closeUp && clonePreview)
                NativeFunction.Natives.TASK_LOOK_AT_COORD(previewPed, position.X, position.Y, position.Z, -1, 0, 2);
            NativeFunction.Natives.SET_CAM_FOV(featureCamera, closeUp ? 32f : 45f);
            NativeFunction.Natives.RENDER_SCRIPT_CAMS(true, false, 0, true, false);
        }

        private void AddPreviewAngle()
        {
            var item = Number("Girar prévia", 0, 345, previewAngle, angle =>
            {
                previewAngle = angle;
                StartCamera(false);
            }, 15);
            item.AllowWrapAround = true;
            item.Formatter = angle => angle + "°";
        }

        private void StopCamera()
        {
            if (featureCamera == 0) return;
            NativeFunction.Natives.RENDER_SCRIPT_CAMS(false, false, 0, true, false);
            NativeFunction.Natives.DESTROY_CAM(featureCamera, false);
            featureCamera = 0;
        }

        private void Confirm(string text, Action confirm, Action cancel)
        {
            Page(text, cancel);
            Row("Confirmar", confirm, tone: ActionTone.Destructive);
        }

        private void Page(string title, Action back = null, string key = null)
        {
            if (pageKey != null && menu.HasSelection)
                pagePositions[pageKey] = (menu.CurrentSelection, menu.FirstItemOnScreen);
            menu.ClearPage();
            menu.SubtitleText = title;
            menu.Back = back;
            pageKey = key ?? title;
            menu.RestorePosition(pagePositions.TryGetValue(pageKey, out var position)
                ? position : ((int Selected, int First)?)null);
        }

        private void Row(string label, Action action, bool enabled = true,
            Badge right = Badge.None, ActionTone tone = ActionTone.Neutral, string rightLabel = null)
        {
            var item = MenuPalette.Item(label, tone, enabled: action != null && enabled);
            item.RightBadge = right;
            item.RightLabel = rightLabel;
            if (action != null) item.Activated += (sender, selected) => { if (item.Enabled) action(); };
            menu.AddItem(item);
        }

        private UIMenuNumericScrollerItem<int> Number(string label, int min, int max, int value, Action<int> set, int step = 1)
        {
            max = Math.Max(min, max);
            var item = new UIMenuNumericScrollerItem<int>(label, "", min, Math.Max(min + step, max), step)
                { Value = Math.Max(min, Math.Min(max, value)), Enabled = max > min, AllowWrapAround = false };
            MenuPalette.Apply(item);
            item.IndexChanged += (sender, oldIndex, newIndex) => set(item.Value);
            menu.AddItem(item);
            return item;
        }

        private UIMenuNumericScrollerItem<int> Bar(string label, int min, int max, int value, Action<int> set)
        {
            var item = Number(label, min, max, value, set);
            item.SliderBar = new UIMenuScrollerSliderBar { ForegroundColor = MenuPalette.Accent };
            if (min < 0) item.SliderBar.Markers.Add(new UIMenuScrollerSliderBarMarker(0.5f));
            return item;
        }

        private void Choice(string label, int[] values, int value, Action<int> set, Func<int, string> name = null)
        {
            var item = Number(label, 0, values.Length - 1, Array.IndexOf(values, value), index => set(values[index]));
            item.Formatter = index => name?.Invoke(values[index]) ?? values[index].ToString();
        }

        private static string Prompt(string initial)
        {
            NativeFunction.Natives.DISPLAY_ONSCREEN_KEYBOARD(1, "FMMC_KEY_TIP8", "", initial, "", "", "", 40);
            var timeout = Stopwatch.StartNew();
            while (timeout.Elapsed.TotalMinutes < 2)
            {
                int status = NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>();
                if (status == 1)
                {
                    var result = NativeFunction.CallByHash<IntPtr>(0x8362B09B91893647UL);
                    string value = Marshal.PtrToStringAnsi(result)?.Trim();
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
                if (status != 0) return null;
                GameFiber.Yield();
            }
            NativeFunction.CallByHash<uint>(0x8817605C2BA76200UL);
            return null;
        }

        private static void SaveFailed() => Game.DisplayNotification("Não foi possível salvar as alterações do médico.");
    }
}
