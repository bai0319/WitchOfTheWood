using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// 线索日记本系统 - 20页翻页式日记（8线索+4药水+8植物）
/// </summary>
public class ClueDiarySystem : MonoBehaviour
{
    public static ClueDiarySystem instance;

    public enum PageCategory { Clue, Potion, Plant }

    [System.Serializable]
    public class DiaryPage
    {
        public PageCategory category;
        public string title;
        public string content;
    }

    [Header("UI引用")]
    public GameObject clueDiaryPanel;
    public TextMeshProUGUI pageTitleText;
    public TextMeshProUGUI pageContentText;
    public TextMeshProUGUI pageCategoryText;
    public TextMeshProUGUI pageNumberText;
    public Button prevPageButton;
    public Button nextPageButton;
    public Button tabClueButton;
    public Button tabPotionButton;
    public Button tabPlantButton;

    private List<DiaryPage> allPages = new List<DiaryPage>();
    private List<DiaryPage> filteredPages = new List<DiaryPage>();
    private int currentPageIndex = 0;
    private PageCategory currentFilter = PageCategory.Clue;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializePages();
        if (clueDiaryPanel != null)
            clueDiaryPanel.SetActive(false);
        FilterAndShowPages(PageCategory.Clue);
    }

    void OnEnable()
    {
        // 如果还没有初始化页面数据，先初始化
        if (allPages.Count == 0)
        {
            InitializePages();
        }
        
        // 刷新显示
        UpdatePageDisplay();
    }

    /// <summary>
    /// 显示日记本面板（带刷新）
    /// </summary>
    public void ShowDiary()
    {
        if (clueDiaryPanel != null)
        {
            clueDiaryPanel.SetActive(true);
            UpdatePageDisplay();
        }
        else
        {
            Debug.LogWarning("ClueDiarySystem: clueDiaryPanel reference is null! Please generate UI via UIGenerator.");
        }
    }

    /// <summary>
    /// 初始化所有20页内容
    /// </summary>
    void InitializePages()
    {
        allPages.Clear();

        // ========== 8 Clue Pages ==========

        // 线索① - 草药笔记-黄花鸢尾：E逐渐认同M的堕胎工作，成为前哨
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 1 - Herbal Notes · Yellow Iris",
            content = "Phase 1 - Found in Herbal Notes\n\nShe said she'd made up her mind, that what I was doing was right. The joy of new life cannot erase the poverty that comes with it—it's just that she didn't yet have the courage to watch a little life pass away...\n\nShe placed the yellow iris in my hand, saying she'd be my lookout from now on. I wanted to thank her, but I didn't know what to say.\n\nThis marks the beginning of E and M's secret collaboration. E, a midwife, acknowledges the necessity of M's work helping women in need."
        });

        // 线索② - 庇护物-绳结：A初次与M相遇，请求收留过冬的对话选项
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 2 - Token of Shelter · Knot",
            content = "Phase 1 - Story Dialogue Choice\n\nA's first encounter with M. A asked M to take them in for the winter. M demanded something as collateral for shelter.\n\n「Nutmeg (worth 50 shillings)」\nM refuses: \"Everyone has nutmeg. I need something that belongs only to you.\"\n\n「Knot」\nM accepts it, examining the knot in silence for a long time: \"...Come in.\"\n\nThe knot represents a personal connection—a memory that bridges past and present."
        });

        // 线索③ - 林中小屋探索-门挂绳结：可交互物品，揭示绳结的来历
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 3 - Cabin in the Woods · Door Hanging Knot",
            content = "Phase 1 - Interactable Object\n\nThough the craftsmanship is rough, one can see the maker's clumsy efforts. The way the knot is tied seems somehow familiar...\n\nThis knot was made by E during the period when she was learning from M. The same tying technique appears in the shelter token A gave M, suggesting a deeper connection between all these events."
        });

        // 线索④ - 草药柜-洋甘菊：贝恩难产事件，A发现名单和妈妈ELSPETH的名字
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 4 - Herbal Cabinet · Chamomile",
            content = "Phase 2 - Critical Plot Point\n\n(1) Bain was having a difficult labor. M told A to get chamomile from the top of the herbal cabinet to soothe Bain's emotions.\n\n(2) While rummaging, A touched a list hidden among the herbs.\n\n(3) As A brought the list down, a loose page fell to the ground. A picked it up.\n\n(4) The page reads: \"Today she asked me what name I'd give her if she ever had a daughter. I said ABBYE. She asked why. I told her it was my mother's name.\"\n\n(5) Shocked, A flipped through the trade ledger and found her own mother ELSPETH's name among the many entries.\n\nA now holds the witch hunter's requested list but faces a moral dilemma—her own mother's name is on it."
        });

        // 线索⑤ - 草药笔记-鼠尾草：帮助堕胎的事被发现，约定在枯槐树下逃跑
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 5 - Herbal Notes · Sage",
            content = "Phase 1 - Found in Herbal Notes\n\nIt was discovered. The witch hunters are coming. She held my hand, trembling constantly. We made a pact—to meet under the withered locust tree at full moon.\n\nThe secret of helping village women terminate unwanted pregnancies has been exposed. M and E plan to flee together, marking a turning point in their relationship."
        });

        // 线索⑥ - 枯槐树的布条：可交互物品，紫色布条标记约定地点
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 6 - Cloth from the Withered Tree",
            content = "Phase 3 - Interactable Object\n\nA purple cloth tied to a dead branch, seems to be some kind of marker, already worn and tattered...\n\nThis is the marker M and E agreed upon for their meeting point. The cloth has weathered with time, symbolizing the broken promise and faded hope of their escape plan."
        });

        // 线索⑦ - 草药笔记-荨麻：E没有赴约，M被抓火刑，大雪逃生，两人决裂
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 7 - Herbal Notes · Nettle",
            content = "Phase 1 - Found in Herbal Notes\n\nShe never came... Our whereabouts were exposed. They tied me to a tree, intending to burn me alive. Fortunately, a heavy snowfall extinguished the flames. I never saw her again.\n\nE did not keep their appointment. M was captured alone during her escape attempt and sentenced to burn at the stake. A miraculous snowstorm saved her life, but the betrayal shattered their bond forever. E was never suspected because only M performed the abortions—E was merely the lookout."
        });

        // 线索⑧ - 湖边水刑地：可交互地点，还原E的死因
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = "Clue 8 - Lakeside Ordeal Ground",
            content = "Phase 3 - Interactive Location\n\nAfter interacting, A's memories reveal the cause of E's death.\n\nFollowing their split, E continued midwifery in the village. But babies delivered by her began dying one after another. She was accused of being a witch.\n\nIn the end, to prove her innocence, she voluntarily underwent trial by water and drowned. In Christian tradition, sinking to the bottom means the soul is pure—a tragic end to a woman who only wanted to help."
        });

        // ========== 4 Potion Pages ==========
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Potion,
            title = "Pain Relief Potion",
            content = "Required Herbs: Calendula ×2 + Sage ×1\n\nPoints Gained: 5\n\nOrder Frequency: High\n\nEffects: Relieves pain, suitable for trauma, headaches, joint pain, etc. Calendula has anti-inflammatory and analgesic properties; sage soothes the nerves."
        });

        allPages.Add(new DiaryPage
        {
            category = PageCategory.Potion,
            title = "Sleeping Draught",
            content = "Required Herbs: Chamomile ×2 + Lavender ×1\n\nPoints Gained: 8\n\nOrder Frequency: Medium\n\nEffects: Helps with sleep, relieves anxiety and tension. Chamomile has calming and sedative effects; lavender's fragrance relaxes body and mind."
        });

        allPages.Add(new DiaryPage
        {
            category = PageCategory.Potion,
            title = "Healing Salve",
            content = "Required Herbs: Calendula ×1 + Willow Bark ×2 + Comfrey ×1\n\nPoints Gained: 12\n\nOrder Frequency: Low\n\nEffects: Promotes wound healing, accelerates bodily recovery. Calendula reduces inflammation; willow bark relieves pain and reduces fever; comfrey promotes tissue regeneration."
        });

        allPages.Add(new DiaryPage
        {
            category = PageCategory.Potion,
            title = "Warming Tonic",
            content = "Required Herbs: Sage ×2 + Thyme ×2\n\nPoints Gained: 6\n\nOrder Frequency: High\n\nEffects: Dispels cold, warms the body, prevents illness. Sage warms the center and dispels cold; thyme strengthens immunity."
        });

        // ========== 8 Plant Pages ==========

        // 鼠尾草（蓝花鼠尾草）- 唇形科鼠尾草属，多年生芳香草本/半灌木
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Sage (Salvia officinalis)",
            content = "Scientific Name: Salvia officinalis\n\nFamily: Lamiaceae (Mint family), perennial aromatic herb/subshrub.\n\nCharacteristics:\n- Height: 30-90cm\n- Leaves: Gray-green, oblong, wrinkled surface, strongly aromatic\n- Flowers: Summer blooming, whorls of blue-purple labiate flowers\n\nMedicinal Value:\n- Traditional: Antibacterial, anti-inflammatory, aids digestion, relieves sore throat, regulates menstruation, improves menopausal night sweats\n- Modern: Essential oil for aromatherapy; dried leaves for tea or seasoning; medicinal ingredients for oral care and digestive preparations\n\nHarvest Period:\n1) Best: Mature plant, leaves fully expanded, before flowering (May-June). Leaves contain highest essential oil (1,8-cineole, camphor, thujone), strongest antibacterial/anti-inflammatory/antioxidant activity\n2) Secondary: Early flowering stage (June-July), when flower spikes just open. Leaves still rich in essential oil; flowers contain flavonoids enhancing antioxidant and antispasmodic effects"
        });

        // 薰衣草 - 唇形科
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Lavender (Lavandula angustifolia)",
            content = "Scientific Name: Lavandula angustifolia (syn. L. officinalis)\n\nFamily: Lamiaceae (Mint family)\n\nCharacteristics:\n- Leaves: Narrow linear, gray-green with white fuzz\n- Flowers: Compact spikes, strong fragrance\n- Growth: Small shrub with neat growth habit\n- Key Difference from Sage: Leaf shape, hairiness, fragrance intensity\n\nNative Distribution: Southern Europe (France, Italy, Spain), wild at 800-1500m elevation, cold-hardy and drought-tolerant, prefers sunny dry slopes\n\nMedicinal Value:\n- Main parts: Flowers and inflorescences\n- Active ingredients: Linalool, linalyl acetate and other volatile oils\n- Effects: Sedative, anxiolytic, antibacterial, antispasmodic\n- Best harvest: Early flowering stage (3-5 days after first blooms), when essential oil content is highest"
        });

        // 百里香 - 唇形科百里香属，多年生芳香半灌木
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Thyme (Thymus vulgaris)",
            content = "Family: Lamiaceae (Mint family)\n\nCharacteristics:\n- Growth habit: Creeping, prostrate, low-growing\n- Leaves: Tiny, small leaves\n- Scent: Whole plant has strong herbal aroma\n- Form: Spreading mat growth\n\nEuropean medicinal thyme (common thyme) is a perennial aromatic subshrub native to the Mediterranean region, widely distributed in European mountains. Tolerates poor soil, prefers sunny dry environments, can grow on rocky mountain slopes."
        });

        // 金盏花 - 菊科，一年生草本
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Calendula (Calendula officinalis)",
            content = "Scientific Name: Calendula officinalis\n\nFamily: Asteraceae (Daisy family)\n\nCharacteristics:\n- Flowers: Golden/orange petals in layered disc shape, large and conspicuous\n- Leaves: Fuzzy, serrated edges\n- Growth: Annual herb\n\nMedicinal calendula, also known as pot marigold, is native to southern Europe. Cold-hardy and tolerant of poor soil, it can grow wild in mountainous areas. It is a traditional European medicinal plant."
        });

        // 荨麻 - 荨麻科，野外林下常见
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Nettle",
            content = "Family: Urticaceae (Nettle family)\n\nCharacteristics:\n- Stinging hairs: Covered on stems and leaves, causes itching on contact\n- Leaves: Opposite arrangement, deeply serrated edges\n- Habitat: Common in wild forest undergrowth, not an ornamental flower\n\nMature Plant (before flowering, early June):\n- Height: 60-120cm\n- Stems: Sturdy, distinctly four-angled\n- Leaves: Dark green, thick, sharply serrated\n- Axillary buds: Plump but not yet open\n- Medicinal value: Highest in leaves at this stage"
        });

        // 康复草（正式名：聚合草）- 紫草科，药用牧草
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Comfrey (Healing Herb)",
            content = "Scientific Name: Symphytum officinale\n\nFamily: Boraginaceae (Borage family)\n\nCharacteristics:\n- Leaves: Large, thick, rough and hairy\n- Flowers: Small bell-shaped, purple-red/pale purple\n- Recognition: Medicinal forage plant, easily identifiable by oversized leaves\n\nJuvenile Stage (Seedling, before flowering):\n- Height: 5-15cm, prostrate or ascending\n- Stems: Four-angled, purple-red or green, covered in fine hairs, easily rooting at nodes\n- Leaves: Opposite, ovate to lanceolate, 1.5-4cm long, entire or slightly toothed\n- Flowers: None yet, only basal leaves and short stems\n- Medicinal content: High levels of flavonoids and phenolic acids"
        });

        // 柳树皮（白柳）- 杨柳科，大乔木
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Willow Bark (Salix alba)",
            content = "Scientific Name: Salix alba L.\n\nFamily: Salicaceae (Willow family)\n\nDistribution:\n- Throughout European riverbanks and wetlands\n- Common in deep mountain valleys\n- Cold-hardy to -25°C, deciduous in winter\n\nMorphology:\n- Size: Large tree, 20-30m tall\n- Bark: Deeply fissured, gray-brown\n- Leaves: Lanceolate, silvery-white fuzz beneath\n\nMedicinal Use:\n- Authentic in European pharmacopeia\n- Salicin content: 7-10%\n- Primary use: Analgesic and antipyretic"
        });

        // 黄花鸢尾（剧情植物）- 鸢尾科，水生/湿生植物
        allPages.Add(new DiaryPage
        {
            category = PageCategory.Plant,
            title = "Yellow Iris (Story Plant)",
            content = "Family: Iridaceae (Iris family)\n\nCharacteristics:\n- Habitat: Aquatic/wetland plant\n- Leaves: Long sword-shaped, erect like a fan\n- Flowers: Large yellow flowers with patterned petals\n- Growing location: Near water and wetlands\n\nStory Significance:\nThis is the plant M gave to E when they first formed their partnership. E placed the yellow iris in M's hand and said she would be M's lookout from then on. It symbolizes their friendship and marks the beginning of their secret collaboration."
        });
    }

    /// <summary>
    /// 根据分类筛选并显示页面
    /// </summary>
    void FilterAndShowPages(PageCategory category)
    {
        currentFilter = category;
        filteredPages = allPages.FindAll(p => p.category == category);
        currentPageIndex = 0;
        UpdatePageDisplay();
    }

    /// <summary>
    /// 更新页面显示
    /// </summary>
    public void UpdatePageDisplay()
    {
        if (filteredPages.Count == 0) return;

        DiaryPage page = filteredPages[currentPageIndex];

        if (pageTitleText != null)
            pageTitleText.text = page.title;

        if (pageContentText != null)
        {
            pageContentText.text = page.content;
            LayoutRebuilder.ForceRebuildLayoutImmediate(pageContentText.rectTransform);
        }

        if (pageCategoryText != null)
        {
            switch (page.category)
            {
                case PageCategory.Clue: pageCategoryText.text = "Clues"; break;
                case PageCategory.Potion: pageCategoryText.text = "Potions"; break;
                case PageCategory.Plant: pageCategoryText.text = "Plants"; break;
            }
        }

        if (pageNumberText != null)
            pageNumberText.text = $"{currentPageIndex + 1} / {filteredPages.Count}";

        if (prevPageButton != null)
            prevPageButton.interactable = currentPageIndex > 0;

        if (nextPageButton != null)
            nextPageButton.interactable = currentPageIndex < filteredPages.Count - 1;
    }

    /// <summary>
    /// 上一页
    /// </summary>
    public void GoToPrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay();
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    public void GoToNextPage()
    {
        if (currentPageIndex < filteredPages.Count - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();
        }
    }

    /// <summary>
    /// 切换到线索分类
    /// </summary>
    public void ShowCluePages()
    {
        FilterAndShowPages(PageCategory.Clue);
    }

    /// <summary>
    /// 切换到药水分类
    /// </summary>
    public void ShowPotionPages()
    {
        FilterAndShowPages(PageCategory.Potion);
    }

    /// <summary>
    /// 切换到植物分类
    /// </summary>
    public void ShowPlantPages()
    {
        FilterAndShowPages(PageCategory.Plant);
    }

    /// <summary>
    /// 添加新线索（动态添加）
    /// </summary>
    public void AddClue(string title, string content)
    {
        // 检查是否已存在
        if (allPages.Exists(p => p.category == PageCategory.Clue && p.title == title))
            return;

        allPages.Add(new DiaryPage
        {
            category = PageCategory.Clue,
            title = title,
            content = content
        });

        // 如果当前在线索分类，刷新显示
        if (currentFilter == PageCategory.Clue)
        {
            FilterAndShowPages(PageCategory.Clue);
        }
    }

    /// <summary>
    /// 从可交互对象添加线索（兼容旧代码）
    /// </summary>
    public void AddClueFromInteraction(InteractableObject interactable)
    {
        if (interactable == null) return;

        string title = $"关于「{interactable.objectName}」";
        string content = interactable.clueText;

        AddClue(title, content);
    }
}
