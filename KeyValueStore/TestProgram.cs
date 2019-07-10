using System;
using System.Collections.Generic;

namespace KeyValueStore
{
    class TestProgram
    {
        static void Main(string[] args)
        {
            // Can have a path to where you want the DB files stored. Like -> new KeyValueStore("../../SomeFolder/"); The folderPath needs to exist. 
            // Can also be told if it should be flushed/Create a fresh Database which might be useful for debugging. Like -> new KeyValueStore(shouldFlush: true) / new KeyValueStore("../../SomeFolder/", true); 
            // The 4 ways it can be created.
            //IKeyValueStore store = new KeyValueStore();
            //IKeyValueStore store = new KeyValueStore("../../SomeFolder/");
            //IKeyValueStore store = new KeyValueStore("../../SomeFolder/", true);
            IKeyValueStore store = new KeyValueStore(shouldUseTemp: true);

            var exercise1 = new Exercise()
            {
                ExerciseName = "Bench",
                Reps = 8,
                Rpe = 12,
                MuscleGroups = MuscleGroups.Chest
            };

            store.Store("Ex", exercise1);
            var ex = store.Fetch<Exercise>("Ex");

            Console.WriteLine(ex == null ? "isnull" : "nonull");
            Console.ReadLine();
        }
    }


    public class TrainingDay
    {
        public List<Exercise> Exercises { get; set; }
        public string User { get; set; }
        public string Coach { get; set; }
    }

    public class Exercise
    {
        public string ExerciseName { get; set; }
        public MuscleGroups MuscleGroups { get; set; }
        public int Rpe { get; set; }
        public int Reps { get; set; }
    }

    public enum MuscleGroups
    {
        Arms, 
        ArmsButBetter,
        Triceps,
        Biceps,
        Chest,
        Titties,
        NotLegs
    }
}
