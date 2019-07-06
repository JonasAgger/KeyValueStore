using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            IKeyValueStore store = new KeyValueStore(shouldFlush: true);

            var exercise1 = new Exercise()
            {
                ExerciseName = "Bench",
                Reps = 8,
                Rpe = 12,
                MuscleGroups = MuscleGroups.Chest
            };
            
            var exercise2 = new Exercise()
            {
                ExerciseName = "Squat",
                Reps = 8,
                Rpe = 11,
                MuscleGroups = MuscleGroups.NotLegs
            };

            var exercise3 = new Exercise()
            {
                ExerciseName = "Dedlift",
                Reps = 8,
                Rpe = 11,
                MuscleGroups = MuscleGroups.ArmsButBetter
            };

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < 10000; i++)
            {
                store.Store($"Exercise{i}", exercise1);
            }

            Console.WriteLine(stopWatch.ElapsedMilliseconds);

            stopWatch.Restart();
            var ex = store.Fetch<Exercise>("Exercise8423");
            stopWatch.Stop();
            Console.WriteLine(stopWatch.ElapsedMilliseconds);
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
