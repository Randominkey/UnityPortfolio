namespace MasterFramework.Core
{
    [System.Serializable]
    public struct LessonSessionContext
    {
        public string semId;
        public string topCorsId;
        public string levelCode;
        public string lessonId;
        public string userId;
        public string accountType; // "t" = teacher, "s" = student
        public int comId;          // Component ID (e.g., 1515)
        
        public bool IsTeacher => accountType == "t";
    }
}
