﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Den.Tools.GUI;

namespace Den.Tools
{
	public class LogWindow : EditorWindow
	{
		UI toolbarUI = new UI();
		UI previewUI = UI.ScrolledUI(maxZoom:4, minZoom:0.375f); 

		const int toolbarWidth = 260; //128+4

		long lastEntryTime; //to draw entries in order

		[NonSerialized] List<string> selectedIds = new List<string>();

		bool columnsId;
		Rect displayRect;
		GUIStyle blackLabel;
		GUIStyle blackCenterLabel;

		private void OnGUI () 
		{
			titleContent = new GUIContent(name);

			previewUI.Draw(DrawField, inInspector:false);
			toolbarUI.Draw(DrawToolbar, inInspector:false);
		}

		//[System.NonSerialized] private bool testLogCreated = false;
		private void CreateTestLog ()
		{
			long time = DateTime.Now.Ticks - System.Diagnostics.Process.GetCurrentProcess().StartTime.Ticks;

			Log.enabled = true;
			Log.Add("I'm test entry!", id:null, obj:null);
			Log.Add("Another entry!", id:null, obj:null);
			Log.Add("Enry with fields", id:null, idName:null, obj:null, ("Field1", 1), ("Field2", 2));
			Log.Add("Threaded entry!", id:"Thread 1", obj:null);
			Log.enabled = false;
		}

		private void DrawToolbar ()
		{
			using (Cell.LinePx(20))
			{
				Draw.Button();

				//Graph graph = CurrentGraph;
				//Graph rootGraph = mapMagic.graph;

				//if (mapMagic != null  &&  mapMagic.graph!=graph  &&  mapMagic.graph!=rootGraph) mapMagic = null;

				UI.current.styles.Resize(0.9f);  //shrinking all font sizes

				Draw.Element(UI.current.styles.toolbar);

				using (Cell.RowPx(60))
				{
					Draw.CheckButton(ref Log.enabled, style:UI.current.styles.toolbarButton);
					Draw.Label("Record", style:UI.current.styles.boldLabel);
				}

				using (Cell.RowPx(60))
				{
					if (Draw.Button("", style:UI.current.styles.toolbarButton))
						Log.Clear();
					Draw.Label("Clear", style:UI.current.styles.label);
				}

				using (Cell.RowPx(40))
					if (Draw.Button("Test", style:UI.current.styles.toolbarButton))
						CreateTestLog();

				using (Cell.RowPx(160))
					Draw.Toggle(ref columnsId, "Columns Id");

				using (Cell.RowPx(30)) Draw.Label("Zoom");
				using (Cell.RowPx(100)) Draw.Field(ref previewUI.scrollZoom.zoom.x, "Hor"); 
				using (Cell.RowPx(100)) Draw.Field(ref previewUI.scrollZoom.zoom.x, "Ver"); 

				Cell.EmptyRow();

				using (Cell.RowPx(40))
					if (Draw.Button("To Top", style:UI.current.styles.toolbarButton))
					{
						float scrollX = previewUI.scrollZoom.scroll.x;
						previewUI.scrollZoom.FocusWindowOn( new Vector2(0,0), position.size);
						previewUI.scrollZoom.scroll.x = scrollX;
					}

				using (Cell.RowPx(50))
					if (Draw.Button("To Bottom", style:UI.current.styles.toolbarButton))
					{
						float scrollX = previewUI.scrollZoom.scroll.x;
						previewUI.scrollZoom.FocusWindowOn( new Vector2(0, (GetLastTick()-GetFirstTick())/10000), position.size);
						previewUI.scrollZoom.scroll.x = scrollX;
					}
			}
		}


		protected virtual void DrawField ()
		{
			if (Log.root.subs == null)
				return;

			previewUI.styles.OverridePro(false);

			//previewUI.scrollZoom.allowZoomX = false;

			//styles
			displayRect = new Rect(0, 0, Screen.width, Screen.height);
			blackLabel = new GUIStyle(UnityEditor.EditorStyles.label);
			blackLabel.active.textColor = blackLabel.normal.textColor =  blackLabel.focused.textColor = Color.black;
			blackCenterLabel = new GUIStyle(blackLabel);
			blackCenterLabel.alignment = TextAnchor.MiddleCenter;

			//background
			float backgroundColor = 0.3f;
			Draw.Background(displayRect, new Color(backgroundColor,backgroundColor,backgroundColor));

			//columns
			List<string> allIds = AllIds();
			Dictionary<string,int> idToOffset = new Dictionary<string, int>();
			Dictionary<string, HashSet<string>> idsToNames = new Dictionary<string, HashSet<string>>();

			Draw.StaticAxis(displayRect, 0, true, Color.black); //topmost left line

			int columnWidth = 50;
			float columnWidthScreen = columnWidth * UI.current.scrollZoom.zoom.x;

			for (int i=0; i<allIds.Count; i++)
			{
				string id = allIds[i];
				int posX = i*columnWidth;

				idToOffset.Add(id, posX);

				float posXscreen = UI.current.scrollZoom.ToScreen( new Vector2(posX, 0) ).x;

				using (Cell.Static(posXscreen, 0, columnWidthScreen, displayRect.height))
					if (Draw.Button(visible:false)  &&  Event.current.shift)
					{
						if (selectedIds.Contains(id))
							selectedIds.Remove(id);
						else
							selectedIds.Add(id);
					}

				//using (Cell.Static(posXscreen, 20, columnWidthScreen, 20))
				//	Draw.Label(id, blackCenterLabel);

				Draw.StaticAxis(displayRect, posX+columnWidth, true, Color.black);
			}

			//entries
			int posY = 0;
			foreach (Log.Entry entry in Log.root.subs)
			{
				string id = entry.id;
				int posX = idToOffset[id];
				
				//is it selected?
				bool selected;
				if (selectedIds.Count != 0)
					selected = selectedIds.Contains(id);
				else //enabling all if nothing selected
					selected = true;

				//drawing
				int height = DrawEntry(entry, posX, posY, selected);

				//adding it's name to list of names
				if (entry.idName != null)
				{
					if (!idsToNames.TryGetValue(id, out HashSet<string> idnames))
					{
						idnames = new HashSet<string>();
						idsToNames.Add(id,idnames);
					}
					idnames.Add(entry.idName);
				}

				posY += height;
			}

			//column names
			for (int i=0; i<allIds.Count; i++)
			{
				string id = allIds[i];
				int posX = i*columnWidth;

				float posXscreen = UI.current.scrollZoom.ToScreen( new Vector2(posX, 0) ).x;

				using (Cell.Static(posXscreen, 20, columnWidthScreen, 20))
				{
					if (!idsToNames.TryGetValue(id, out HashSet<string> idnames))
						Draw.Label(id, blackCenterLabel);

					else
						foreach (string idname in idnames)
							using (Cell.LineStd) Draw.Label(idname, blackCenterLabel);
				}
			}
		}


		protected virtual void DrawField_TimeBased ()
		{
			previewUI.scrollZoom.allowZoomX = false;

			//styles
			displayRect = new Rect(0, 0, Screen.width, Screen.height);
			blackLabel = new GUIStyle(UnityEditor.EditorStyles.label);
			blackLabel.active.textColor = blackLabel.normal.textColor =  blackLabel.focused.textColor = Color.black;
			blackCenterLabel = new GUIStyle(blackLabel);
			blackCenterLabel.alignment = TextAnchor.MiddleCenter;

			//background
			float backgroundColor = 0.3f;
			Draw.Background(displayRect, new Color(backgroundColor,backgroundColor,backgroundColor));

			//entries
			List<string> allIds = AllIds();

				long firstTime = GetFirstTick();

				//initial column line
				Draw.StaticAxis(displayRect, 0, true, Color.black);

				for (int i=0; i<allIds.Count; i++)
				{
					//drawing ids
					string id = allIds[i];
					int idWidth = 200;
					int posX = i*idWidth;
					//float posXinternal = UI.current.scrollZoom.ToInternal( new Vector2(posX, 0) ).x;

					float posXscreen = UI.current.scrollZoom.ToScreen( new Vector2(posX, 0) ).x;



					float width = idWidth * UI.current.scrollZoom.zoom.x;
					using (Cell.Static(posXscreen, 20, width, 20))
						Draw.Label(id, blackCenterLabel);

					Draw.StaticAxis(displayRect, posX+idWidth, true, Color.black);

					//entries within this id
					lastEntryTime = 0;

					foreach (Log.Entry entry in Log.root.subs)
					{
						lastEntryTime = Math.Max(lastEntryTime+1, entry.startTicks-firstTime);

						//if (entry.threadName == id)
							//DrawEntry(entry, posX, (int)(lastEntryTime / 10000));
					}
				}
		}


		private int DrawEntry (Log.Entry entry, int posX, int posY, bool selected)
		///Returns the height of the entry
		{
			int width = (int)UnityEditor.EditorStyles.label.CalcSize( new GUIContent(entry.name) ).x + 20;
			//int height = 20; 

			Vector2 pos = UI.current.scrollZoom.ToScreen( new Vector2(posX, posY) );

			//Rect displayRect = new Rect(0, 0, Screen.width, Screen.height);
			//Draw.StaticAxis(displayRect, posY, false, new Color(0,0,0,0.5f));

			//using (Cell.Static(pos.x+2, pos.y, width, 0))
			using (Cell.LineStd)
				using (Cell.Custom(posX+2, 2, width, 20))
			{
				if (!UI.current.layout)
				{
					GUIStyle frameStyle = UI.current.textures.GetElementStyle("LogEntry", 
							borders: new RectOffset(10,2,6,2),
							overflow: new RectOffset(0,0,0,0) );

					GUIStyle frameStyleDisabled = UI.current.textures.GetElementStyle("LogEntryDisabled", 
							borders: new RectOffset(10,2,6,2),
							overflow: new RectOffset(0,0,0,0) );

					Draw.Element(selected ? frameStyle : frameStyleDisabled);
				}

				using (Cell.Padded(2, 2, 2, 0))
					
				{
					if (entry.fieldValues == null  ||  entry.fieldValues.Length == 0)
					{
						using (Cell.LineStd)
							Draw.Label(entry.name);
					}

					else
					{
						using (Cell.LineStd)
							Draw.FoldoutLeft(ref entry.guiExpanded, entry.name, style:UI.current.styles.label);

						if (entry.guiExpanded)
						{
							foreach ((string n,string v) in entry.fieldValues)
								using (Cell.LinePx(17))
								{
									using (Cell.RowRel(0.3f)) Draw.Label(n);
									using (Cell.RowRel(0.7f)) Draw.Label(v, tooltip:v);
								}
						}
					}
				}

				

					

				return (int)Cell.current.pixelSize.y;
			}
		}


		private List<string> AllIds ()
		{
			Dictionary<string,int> usedIds = Log.UsedThreadsNums();

			List<string> orderedIds = new List<string>();
			while (usedIds.Count > 0)
			{
				int firstNum = -1; //int.MaxValue;
				string firstId = null;

				foreach (var kvp in usedIds)
				{
					if (kvp.Value > firstNum)
					{
						firstNum = kvp.Value;
						firstId = kvp.Key;
					}
				}

				usedIds.Remove(firstId);

				orderedIds.Add(firstId);
			}

			if (orderedIds.Contains(Log.defaultId))
				orderedIds.Remove(Log.defaultId);

			orderedIds.Insert(0, Log.defaultId);

			return orderedIds;
		}



		private long GetFirstTick ()
		{
			if (Log.root.subs == null  ||  Log.root.subs.Count == 0)
				return 0;

			return Log.root.subs[Log.root.subs.Count-1].startTicks;
		}

		private long GetLastTick ()
		{
			if (Log.root.subs == null  ||  Log.root.subs.Count == 0)
				return 0;

			return Log.root.subs[0].startTicks;
		}


		[MenuItem ("Window/MapMagic/Log")]
		public static void ShowEditor ()
		{
			EditorWindow.GetWindow<LogWindow>("Log");
		}
	}


	public class LogWindow_StandardGUI : EditorWindow
	{
		const int toolbarHeight = 18;
		const int scrollWidth = 15;
		const int lineHeight = 18;
		const int rowMinWidth = 100;
		const float namesWidthPercent = 0.4f;
		Vector2 scrollPosition;
		bool threadedView = false;
		//int selectedLine = 1;

		float timeWidth = 50;

		GUIStyle labelStyle = null;

		bool groupEnabled = false;
		bool prevEnabled;

		int prevLogCount = 0;

		public void OnInspectorUpdate () 
		{
			if (Log.Count != prevLogCount)
				Repaint();
			prevLogCount = Log.Count;
		}


		public void OnGUI () 
		{
			DrawToolbar();

			Dictionary<string,int> threadToRow = GetIdsToRows();
			DrawHeader(threadToRow);
			DrawList(threadToRow);
		}

		public void DrawToolbar ()
		{
			//toolbar/header
			if (Event.current.type == EventType.Repaint)
				EditorStyles.toolbar.Draw(new Rect(0,0,position.width, toolbarHeight), new GUIContent(), 0);
			
			//Record
			Log.enabled = EditorGUI.Toggle(new Rect(5,0,50, toolbarHeight), Log.enabled, style:EditorStyles.toolbarButton);
			EditorGUI.LabelField(new Rect(9,1,50,toolbarHeight), "Record", style:EditorStyles.miniBoldLabel);

			//Group (not used, just for future)
			bool newGroupEnabled = EditorGUI.Toggle(new Rect(55,0,50,toolbarHeight), groupEnabled, style:EditorStyles.toolbarButton);
			if (newGroupEnabled && groupEnabled) //just pressed
			{ 
				groupEnabled = true; 
			}
			if (!newGroupEnabled && groupEnabled)
			{
				groupEnabled = false;
			}
			EditorGUI.LabelField(new Rect(63,1,50,toolbarHeight), "Group", style:EditorStyles.miniBoldLabel);

			//Clear
			if (UnityEngine.GUI.Button(new Rect(105,0,50,toolbarHeight), "Clear", style:EditorStyles.toolbarButton))
				Log.Clear();

			//threaded view
			threadedView = UnityEngine.GUI.Toggle(new Rect(165,-8,100,35), threadedView, "Threaded view");
		}

		public void DrawHeader (Dictionary<string,int> threadToRow)
		{
			float rowWidth = (position.width-scrollWidth) / threadToRow.Count;
			if (rowWidth < rowMinWidth) rowWidth = rowMinWidth;

			UnityEngine.GUI.BeginScrollView(
				position:new Rect(0, toolbarHeight, position.width-18, lineHeight), 
				scrollPosition:new Vector2(scrollPosition.x,0), 
				viewRect:new Rect(0, 0, threadToRow.Count*rowWidth, lineHeight),
				alwaysShowHorizontal:false,
				alwaysShowVertical:false,
				horizontalScrollbar:GUIStyle.none,
				verticalScrollbar:GUIStyle.none);
			{
				if (labelStyle == null)
					labelStyle = new GUIStyle(UnityEditor.EditorStyles.label); 
				labelStyle.alignment = TextAnchor.UpperLeft;

				Rect rect = new Rect(0, 0, 0, lineHeight);
				GUIContent content = new GUIContent("", "");

				//timestamp
				rect.width = timeWidth;
				content.text = ""+'\u23F0'; content.tooltip = "Timestamp";
				EditorGUI.LabelField(rect, content, labelStyle);

				//name
				rect.x += rect.width;
				content = new GUIContent("Name", "Name");
				EditorGUI.LabelField(rect, content, labelStyle);

				/*foreach (string id in threadToRow.Keys)
				{
					int rowNum = threadToRow[id];
					Rect rect = new Rect(rowNum*rowWidth,1,rowWidth,lineHeight);

					EditorGUI.LabelField(rect, id.ToString(), style:EditorStyles.boldLabel);
				}*/
			}
			UnityEngine.GUI.EndScrollView();
		}

		public void DrawList (Dictionary<string,int> threadToRow)
		// if idToRow is null using non-threaded view (1 row)
		{
			if (Log.root.subs == null)
				return;

			int totalHeight = 0;
			foreach (Log.Entry entry in Log.root.subs)
				totalHeight += GetEntryHeight(entry, recursively:true);

			Rect valsInternalRect = new Rect();
			valsInternalRect.width = position.width - scrollWidth;
			valsInternalRect.height = totalHeight;

			scrollPosition = UnityEngine.GUI.BeginScrollView(
				position:new Rect(0, toolbarHeight, position.width, position.height-toolbarHeight), 
				scrollPosition:scrollPosition, 
				viewRect:valsInternalRect,
				alwaysShowHorizontal:true,
				alwaysShowVertical:true);
			{
				if (labelStyle == null)
					labelStyle = new GUIStyle(UnityEditor.EditorStyles.label); 
				labelStyle.alignment = TextAnchor.UpperLeft;

				//background
				EditorGUI.DrawRect(valsInternalRect, new Color(0.9f, 0.9f, 0.9f));

				int lineNum = 0;
				foreach (Log.Entry entry in Log.root.subs)
					DrawEntry(valsInternalRect, ref lineNum, entry);
			}
			UnityEngine.GUI.EndScrollView();
		}



		public void DrawEntry (Rect listRect, ref int line, Log.Entry entry)
		/// returns the number of lines actually drawn
		{
			Rect rect = new Rect(listRect.x, listRect.y*lineHeight, 0, lineHeight);
			GUIContent content = new GUIContent("", "");

			//line separator
			EditorGUI.DrawRect(new Rect(listRect.x, listRect.y*line, listRect.width, 1), new Color(0.6f,0.6f,0.6f));

			//timestamp
			rect.width = timeWidth;
			string timeString = $" ({(entry.startTicks/(float)System.TimeSpan.TicksPerMillisecond).ToString("0.0")} ms)";
			content.text = timeString; content.tooltip = timeString;
			EditorGUI.LabelField(rect, content, labelStyle);

			//name
			rect.x += rect.width;
			string name = entry.name;
			content = new GUIContent(name, name);
			EditorGUI.LabelField(rect, content, labelStyle);

			//label/foldout
			/*if (!recursively || entry.subs==null)
				EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, lineHeight), content, labelStyle);
			else
				entry.guiExpanded = EditorGUI.Foldout(new Rect(rect.x, rect.y, rect.width, lineHeight), entry.guiExpanded, content);

			//subs
			int counter = 1;
			if (entry.guiExpanded && recursively && entry.subs != null)
			{
				foreach (Log.Entry sub in entry.subs)
					counter += DrawEntry(new Rect(rect.x+20, rect.y+counter*lineHeight, rect.width-20, rect.y-lineHeight), sub, true);
			}*/

			line++;
		}


		public Dictionary<string,int> GetIdsToRows ()
		/// Generates a lut of idName -> row number if thread view is enabled
		{
			HashSet<string> usedIds = Log.UsedThreads();

			Dictionary<string,int> threadToRow = new Dictionary<string,int>(usedIds.Count);
			int counter = 0;

			if (usedIds.Contains(Log.defaultId))
				{ threadToRow.Add(Log.defaultId, 0); counter++; }

			List<string> orderedIds = new List<string>();
			orderedIds.AddRange(usedIds);
			orderedIds.Sort();

			foreach (string id in orderedIds)
				if (!threadToRow.ContainsKey(id))
					{ threadToRow.Add(id, counter); counter++; }

			return threadToRow;
		}


		public void DrawHeaders (Dictionary<string,int> threadToRow)
		{
			float rowWidth = (position.width-scrollWidth) / threadToRow.Count;
			if (rowWidth < rowMinWidth) rowWidth = rowMinWidth;

			UnityEngine.GUI.BeginScrollView(
				position:new Rect(0, toolbarHeight, position.width-18, lineHeight), 
				scrollPosition:new Vector2(scrollPosition.x,0), 
				viewRect:new Rect(0, 0, threadToRow.Count*rowWidth, lineHeight),
				alwaysShowHorizontal:false,
				alwaysShowVertical:false,
				horizontalScrollbar:GUIStyle.none,
				verticalScrollbar:GUIStyle.none);

				foreach (string id in threadToRow.Keys)
				{
					int rowNum = threadToRow[id];
					Rect rect = new Rect(rowNum*rowWidth,1,rowWidth,lineHeight);

					EditorGUI.LabelField(rect, id.ToString(), style:EditorStyles.boldLabel);
				}

			UnityEngine.GUI.EndScrollView();
		}


		public void DrawThreadedValues (Dictionary<string,int> threadToRow)
		// if idToRow is null using non-threaded view (1 row)
		{
			float rowWidth = (position.width-scrollWidth) / threadToRow.Count;
			if (rowWidth < rowMinWidth) rowWidth = rowMinWidth;

			int totalHeight = 0;
			foreach (Log.Entry entry in Log.AllEntries())
				totalHeight += GetEntryHeight(entry);

			Rect valsInternalRect = new Rect();
			valsInternalRect.width = threadToRow.Count*rowWidth;
			valsInternalRect.height = totalHeight;

			scrollPosition = UnityEngine.GUI.BeginScrollView(
				position:new Rect(0, toolbarHeight+lineHeight, position.width, position.height-toolbarHeight-lineHeight), 
				scrollPosition:scrollPosition, 
				viewRect:valsInternalRect,
				alwaysShowHorizontal:true,
				alwaysShowVertical:true);

				if (labelStyle == null)
					labelStyle = new GUIStyle(UnityEditor.EditorStyles.label); 
				labelStyle.alignment = TextAnchor.UpperLeft;

				//background
				EditorGUI.DrawRect(valsInternalRect, new Color(0.9f, 0.9f, 0.9f));

				//row separators
				for (int i=0; i<threadToRow.Count; i++)
					EditorGUI.DrawRect(new Rect(i*rowWidth, valsInternalRect.y, 1, valsInternalRect.size.y), new Color(0.6f,0.6f,0.6f));

				int currHeight = 0;
				foreach (Log.Entry entry in Log.AllEntries())
				{
					int rowNum = threadToRow[entry.id];
					int entryHeight = GetEntryHeight(entry);
					Rect rect = new Rect(rowNum*rowWidth, currHeight, rowWidth, entryHeight);
					DrawEntry(rect, entry);

					currHeight += entryHeight;
				}

			UnityEngine.GUI.EndScrollView();
		}


		public void DrawValues ()
		// if idToRow is null using non-threaded view (1 row)
		{
			if (Log.root.subs == null)
				return;

			int totalHeight = 0;
			foreach (Log.Entry entry in Log.root.subs)
				totalHeight += GetEntryHeight(entry, recursively:true);

			Rect valsInternalRect = new Rect();
			valsInternalRect.width = position.width - scrollWidth;
			valsInternalRect.height = totalHeight;

			scrollPosition = UnityEngine.GUI.BeginScrollView(
				position:new Rect(0, toolbarHeight, position.width, position.height-toolbarHeight), 
				scrollPosition:scrollPosition, 
				viewRect:valsInternalRect,
				alwaysShowHorizontal:true,
				alwaysShowVertical:true);

				if (labelStyle == null)
					labelStyle = new GUIStyle(UnityEditor.EditorStyles.label); 
				labelStyle.alignment = TextAnchor.UpperLeft;

				//background
				EditorGUI.DrawRect(valsInternalRect, new Color(0.9f, 0.9f, 0.9f));

				int currHeight = 0;
				foreach (Log.Entry entry in Log.root.subs)
				{
					int entryHeight = GetEntryHeight(entry, recursively:true);
					Rect rect = new Rect(valsInternalRect.x, currHeight, valsInternalRect.width, lineHeight);
					DrawEntry(rect, entry, recursively:true);

					currHeight += entryHeight;
				}

			UnityEngine.GUI.EndScrollView();
		}


		public int DrawEntry (Rect rect, Log.Entry entry, bool recursively=false)
		/// returns the number of entries actually drawn
		{
			//line separator
			EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0.6f,0.6f,0.6f));

			//what's written
			string name = entry.name;
			if (entry.startTicks != 0) name += $" ({((entry.disposeTicks-entry.startTicks)/(float)System.TimeSpan.TicksPerMillisecond).ToString("0.0")} ms)";
			GUIContent content = new GUIContent(name, name);

			//label/foldout
			if (!recursively || entry.subs==null)
				EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, lineHeight), content, labelStyle);
			else
				entry.guiExpanded = EditorGUI.Foldout(new Rect(rect.x, rect.y, rect.width, lineHeight), entry.guiExpanded, content);

			//subs
			int counter = 1;
			if (entry.guiExpanded && recursively && entry.subs != null)
			{
				foreach (Log.Entry sub in entry.subs)
					counter += DrawEntry(new Rect(rect.x+20, rect.y+counter*lineHeight, rect.width-20, rect.y-lineHeight), sub, true);
			}

			return counter;
		}


		public int GetEntryHeight (Log.Entry entry, bool recursively=false)
		{
			int height = lineHeight;

			if (recursively && entry.subs != null  &&  entry.guiExpanded)
			{
				foreach (Log.Entry sub in entry.subs)
					height += GetEntryHeight(sub, true);
			}

			return height;
		}
	}
}