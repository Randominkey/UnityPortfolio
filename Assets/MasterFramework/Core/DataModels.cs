using System;
using System.Collections.Generic;

namespace MasterFramework.Core
{
    // ==========================================
    // 1. REST API Models (DB Sync)
    // ==========================================

    [Serializable]
    public class LessonData
    {
        public string contentUrl = "";     // Contents.xml URL
        public int studyStatus = 1;        // SERVICE_RUN_MODE (e.g. In-class / Self)
        public string completeDatetime = "";
        public bool isComplete = false;
        public string courseLevelName = "";
        public int weekSeq = 0;
        public ComData comp;

        public LessonData()
        {
            comp = new ComData();
        }
    }

    [Serializable]
    public class ComData
    {
        public string comId = "";          // Component ID
        public string studyDataUrl = "";   // Azure JSON URL
        public List<ActData> actData;

        public ComData()
        {
            actData = new List<ActData>();
        }
    }

    [Serializable]
    public class ActData
    {
        public string actCode = "";
        public string completeDatetime = "";
        public bool isComplete = false;
        public int starPoint = 0;
    }

    [Serializable]
    public class ResponseSasData
    {
        public bool result;
        public string cd;
        public string message;
        public SasData data;
    }

    [Serializable]
    public class SasData
    {
        public string containerName = "";
        public string azureUrl = "";
        public string sasToken = "";
    }

    // ==========================================
    // 2. Azure Blob JSON Storage Models (Session State)
    // ==========================================

    [Serializable]
    public class StageStateItem
    {
        public int stageIndex;
        public string stateData;

        public StageStateItem(int index, string data)
        {
            stageIndex = index;
            stateData = data;
        }
    }

    [Serializable]
    public class StudyJsonData
    {
        public int study_step_idx = 0;     // Current active tab index
        public List<Activity> activities;
        public string complete_date = "";
        public bool is_complete = false;
        public List<StageStateItem> stage_states; // Persistent states for stages

        public StudyJsonData()
        {
            activities = new List<Activity>();
            stage_states = new List<StageStateItem>();
        }

        public string ToJson()
        {
            return UnityEngine.JsonUtility.ToJson(this);
        }

        public static StudyJsonData FromJson(string json)
        {
            return UnityEngine.JsonUtility.FromJson<StudyJsonData>(json);
        }
    }

    [Serializable]
    public class Activity
    {
        public string act_code = "";
        public List<Step> steps;           // For DigUp pages
        public int vod_play_sec = 0;       // Watch time duration
        public int vod_state = 0;          // Video state flags
        public int star_point = 0;
        public List<Quiz> quizes;          // For StackUp quizzes
        public string complete_date = "";
        public bool is_complete = false;

        public Activity()
        {
            steps = new List<Step>();
            quizes = new List<Quiz>();
        }
    }

    [Serializable]
    public class Step
    {
        public int step = 0;
        public bool is_complete = false;
        public string complete_date = "";

        public Step(int idx)
        {
            step = idx;
        }
    }

    [Serializable]
    public class Quiz
    {
        public int quiz_no = 0;
        public int try_count = 0;
        public int hint_count = 0;
        public bool is_complete = false;
        public string complete_date = "";

        public Quiz(int idx)
        {
            quiz_no = idx;
        }
    }
}
