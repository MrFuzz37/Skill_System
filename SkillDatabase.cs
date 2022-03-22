using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SkillSystem;

/// <summary>
/// Custom editor for creating and managing skills
/// </summary>
public class SkillDatabase : EditorWindow
{
	private Sprite defaultIcon;
	private static List<Skill> skillDatabase = new List<Skill>();
	private VisualElement skillsTab;
	private static VisualTreeAsset skillRowTemplate;
	private ListView skillListView;
	private float skillHeight = 40;

	private ScrollView detailsSection;
	private ScrollView tagSection;
	private ScrollView effectSection;
	private ScrollView statusSection;
	private VisualElement largeDisplayIcon;
	private Skill activeSkill;
	private Categories categories;
	private List<EnumField> tagField = new List<EnumField>();
	private List<ObjectField> objectField = new List<ObjectField>();
	private List<ObjectField> statusField = new List<ObjectField>();

	[MenuItem("Tools/Skill Database")]
	public static void Init()
	{
		// Get an existing window or make a new one
		SkillDatabase window = GetWindow<SkillDatabase>();
		
		// Set the title of the window
		window.titleContent = new GUIContent("Skill Database");

		// Set the size of the window
		Vector2 size = new Vector2(825, 600);
		window.maxSize = size;
		window.minSize = size;
	}

	/// <summary>
	/// Basically an update for the GUI editor
	/// </summary>
	public void OnGUI()
	{
		if (activeSkill)
			detailsSection.Q<Label>("TargetCount").text = activeSkill.targets.ToString();
	}

	/// <summary>
	/// When the GUI is ready to populate this is run and populates the editor with elements
	/// </summary>
	public void CreateGUI()
	{
		// Load a uxml file to grab the data from it
		VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/SkillDatabase.uxml");
		skillRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/SkillRowTemplate.uxml");

		// Instantiate the data in the tree
		VisualElement rootFromUXML = visualTree.Instantiate();

		// Add the data to the current window element
		rootVisualElement.Add(rootFromUXML);

		// load in a stylesheet
		StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/SkillDatabase.uss");

		// add the style sheet to the current window
		rootVisualElement.styleSheets.Add(styleSheet);

		// set the default icon
		defaultIcon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Art/10.png", typeof(Sprite));

		// Load all the skills in assets
		LoadAllSkills();

		skillsTab = rootVisualElement.Q<VisualElement>("SkillsTab");
		GenerateListView();

		detailsSection = rootVisualElement.Q<ScrollView>("ScrollView_Details");
		detailsSection.style.visibility = Visibility.Hidden;
		largeDisplayIcon = detailsSection.Q<VisualElement>("Icon");

		tagSection = rootVisualElement.Q<ScrollView>("TagView");
		tagSection.style.visibility = Visibility.Hidden;

		effectSection = rootVisualElement.Q<ScrollView>("EffectView");
		effectSection.style.visibility = Visibility.Hidden;

		statusSection = rootVisualElement.Q<ScrollView>("StatusView");
		statusSection.style.visibility = Visibility.Hidden;

		// Update the skill list view when changing the skill name
		detailsSection.Q<TextField>("SkillName").RegisterValueChangedCallback(evt =>
		{
			activeSkill.skillName = evt.newValue;
			skillListView.Rebuild();

			// grab the reference to the current asset we are adjusting
			string path = AssetDatabase.GetAssetPath(activeSkill);
			AssetDatabase.RenameAsset(path, evt.newValue);
		});

		// Update the skill list view when changing the skill icon
		detailsSection.Q<ObjectField>("SkillIcon").RegisterValueChangedCallback(evt =>
		{
			Sprite newSprite = evt.newValue as Sprite;
			activeSkill.skillImage = newSprite == null ? defaultIcon : newSprite;
			largeDisplayIcon.style.backgroundImage = newSprite == null ? defaultIcon.texture : newSprite.texture;
			skillListView.Rebuild();
		});

		// Callback for all buttons
		rootVisualElement.Q<Button>("Btn_AddSkill").clicked += AddSkill_OnClick;
		rootVisualElement.Q<Button>("Btn_DeleteSkill").clicked += DeleteSkill_OnClick;
		rootVisualElement.Q<Button>("Btn_SaveSkill").clicked += SaveSkill_OnClick;
		rootVisualElement.Q<Button>("Btn_AddTag").clicked += AddTag_OnClick;
		rootVisualElement.Q<Button>("Btn_DeleteTag").clicked += DeleteTag_OnClick;
		rootVisualElement.Q<Button>("Btn_AddEffect").clicked += AddEffect_OnClick;
		rootVisualElement.Q<Button>("Btn_DeleteEffect").clicked += DeleteEffect_OnClick;
		rootVisualElement.Q<Button>("Btn_AddStatus").clicked += AddStatus_OnClick;
		rootVisualElement.Q<Button>("Btn_DeleteStatus").clicked += DeleteStatus_OnClick;
	}

	#region Button Callbacks
	private void DeleteSkill_OnClick()
	{
		string path = AssetDatabase.GetAssetPath(activeSkill);
		AssetDatabase.DeleteAsset(path);

		tagSection.Clear();
		effectSection.Clear();
		statusSection.Clear();

		skillDatabase.Remove(activeSkill);
		activeSkill = null;
		skillListView.Rebuild();

		detailsSection.style.visibility = Visibility.Hidden;
	}

	private void SaveSkill_OnClick()
	{
		// save the tags
		activeSkill.tags.Clear();
		activeSkill.effects.Clear();
		activeSkill.statusEffects.Clear();

		for (int i = 0; i < tagSection.childCount; i++)
		{
			activeSkill.tags.Add((Categories)tagField[i].value);
		}

		for (int i = 0; i < effectSection.childCount; i++)
		{
			activeSkill.effects.Add((Effect)objectField[i].value);
		}

		for (int i = 0; i < statusSection.childCount; i++)
		{
			activeSkill.statusEffects.Add((Status)statusField[i].value);
		}
	}

	private void AddSkill_OnClick()
	{
		// create an instance of a new skill
		Skill newSkill = CreateInstance<Skill>();
		newSkill.skillName = $"New_Skill";
		newSkill.skillImage = defaultIcon;

		AssetDatabase.CreateAsset(newSkill, $"Assets/Skill System/Skills/{newSkill.ID}.asset");

		skillDatabase.Add(newSkill);

		skillListView.Rebuild();
		skillListView.style.height = skillDatabase.Count * skillHeight;
	}

	private void AddTag_OnClick()
	{
		// create a tag space
		EnumField ef = new EnumField("Tag", categories);
		tagField.Add(ef);
		tagField[tagField.Count - 1].label = "Tag " + tagSection.childCount.ToString();
		tagSection.style.visibility = Visibility.Visible;
		tagSection.Add(tagField[tagField.Count - 1]);
	}

	private void AddEffect_OnClick()
	{
		// create an effect space
		ObjectField ef = new ObjectField("Effect");
		ef.objectType = typeof(Effect);
		ef.name = "Effect" + effectSection.childCount;
		objectField.Add(ef);
		objectField[objectField.Count - 1].label = "Effect " + effectSection.childCount.ToString();
		effectSection.style.visibility = Visibility.Visible;
		effectSection.Add(objectField[objectField.Count - 1]);
	}

	private void DeleteEffect_OnClick()
	{
		// create a tag space
		if (effectSection.childCount > 0)
		{
			effectSection.RemoveAt(effectSection.childCount - 1);
		}
	}

	private void AddStatus_OnClick()
	{
		// create an effect space
		ObjectField st = new ObjectField("Status");
		st.objectType = typeof(Status);
		st.name = "Status" + statusSection.childCount;
		statusField.Add(st);
		statusField[statusField.Count - 1].label = "Status " + statusSection.childCount.ToString();
		statusSection.style.visibility = Visibility.Visible;
		statusSection.Add(statusField[statusField.Count - 1]);
	}

	private void DeleteStatus_OnClick()
	{
		// create a tag space
		if (statusSection.childCount > 0)
		{
			statusSection.RemoveAt(statusSection.childCount - 1);
		}
	}

	private void DeleteTag_OnClick()
	{
		// create a tag space
		if (tagSection.childCount > 0)
		{
			tagSection.RemoveAt(tagSection.childCount - 1);
		}
	}
	#endregion

	/// <summary>
	/// Generates the list of skills by finding them in assets
	/// </summary>
	private void GenerateListView()
	{
		// we clone the template for each skill
		Func<VisualElement> makeSkill = () => skillRowTemplate.CloneTree();

		// and we bind the skill properties to the template
		Action<VisualElement, int> bindSkill = (e, i) =>
		{
			e.Q<VisualElement>("Icon").style.backgroundImage = skillDatabase[i] == null ? defaultIcon.texture : skillDatabase[i].skillImage.texture;
			e.Q<Label>("Name").text = skillDatabase[i].skillName;
		};

		// creates the list view with the creation and binding delegates
		skillListView = new ListView(skillDatabase, 35, makeSkill, bindSkill);
		skillListView.selectionType = SelectionType.Single;

		// adjust the height of the list view based on how big the database is
		skillListView.style.height = skillDatabase.Count * skillHeight;
		skillsTab.Add(skillListView);

		skillListView.onSelectionChange += SkillListView_onSelectionChange;
	}

	/// <summary>
	/// Run when a different skill is selected in the list
	/// </summary>
	/// <param name="selectedSkills"></param>
	private void SkillListView_onSelectionChange(IEnumerable<object> selectedSkills)
	{
		activeSkill = (Skill)selectedSkills.First();

		SerializedObject so = new SerializedObject(activeSkill);
		detailsSection.Bind(so);

		if (activeSkill.skillImage != null)
		{
			largeDisplayIcon.style.backgroundImage = activeSkill.skillImage.texture;
		}
		LoadEffects(activeSkill);
		LoadStatus(activeSkill);
		LoadTags(activeSkill);

		detailsSection.style.visibility = Visibility.Visible;
	}

	/// <summary>
	/// Load all the effects on an already created skill if any
	/// </summary>
	private void LoadEffects(Skill s)
	{
		if (s.effects == null) { return; }

		effectSection.Clear();
		objectField.Clear();

		for (int i = 0; i < activeSkill.effects.Count; i++)
		{
			// create a effect space
			ObjectField ef = new ObjectField("Effect");
			ef.objectType = typeof(Effect);
			ef.value = activeSkill.effects[i];
			objectField.Add(ef);
			objectField[objectField.Count - 1].label = "Effect " + effectSection.childCount.ToString();
			effectSection.style.visibility = Visibility.Visible;
			effectSection.Add(objectField[objectField.Count - 1]);
		}
	}

	/// <summary>
	/// Load all the status effects on an already created skill if any
	/// </summary>
	private void LoadStatus(Skill s)
	{
		if (s.statusEffects == null) { return; }

		statusSection.Clear();
		statusField.Clear();

		for (int i = 0; i < activeSkill.statusEffects.Count; i++)
		{
			// create a effect space
			ObjectField st = new ObjectField("Status");
			st.objectType = typeof(Status);
			st.value = activeSkill.statusEffects[i];
			statusField.Add(st);
			statusField[statusField.Count - 1].label = "Status " + statusSection.childCount.ToString();
			statusSection.style.visibility = Visibility.Visible;
			statusSection.Add(statusField[statusField.Count - 1]);
		}
	}

	/// <summary>
	/// Load all the tags on an already created skill if any
	/// </summary>
	private void LoadTags(Skill s)
	{
		if (s.effects == null) { return; }

		tagSection.Clear();
		tagField.Clear();

		for (int i = 0; i < activeSkill.tags.Count; i++)
		{
			// create a tag space
			EnumField ef = new EnumField("Tag", categories);
			ef.value = activeSkill.tags[i];
			tagField.Add(ef);
			tagField[tagField.Count - 1].label = "Tag " + tagSection.childCount.ToString();
			tagSection.style.visibility = Visibility.Visible;
			tagSection.Add(tagField[tagField.Count - 1]);
		}
	}

	/// <summary>
	/// Finds all the skills in assets
	/// </summary>
	private void LoadAllSkills()
	{
		skillDatabase.Clear();

		// search assets for type of Skill 
		var guids = AssetDatabase.FindAssets("t:Skill");

		for (int i = 0; i < guids.Length; i++)
		{
			// add any skills that are found to the database
			var path = AssetDatabase.GUIDToAssetPath(guids[i]);
			skillDatabase.Add(AssetDatabase.LoadAssetAtPath<Skill>(path));
		}
	}
}
