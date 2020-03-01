using System.Collections.Generic;

namespace PathFinder3D
{
    public class PriorityQueue
    {
        ///*
        private List<Tuple<List<IVertex>, double>> queue;
        const int block = 20;

        public PriorityQueue()
        {
            queue = new List<Tuple<List<IVertex>, double>>(block);
        }

        public void Add(double cost, List<IVertex> elem)
        {
            int i;
            for (i = queue.Count - 1; i >= 0 && cost > queue[i].Item2; i--) ;
            queue.Insert(i + 1, new Tuple<List<IVertex>, double>(elem, cost));
            if (queue.Count - 1 == queue.Capacity)
                queue.Capacity += block;
        }

        public List<IVertex> First()
        {
            return queue[queue.Count - 1].Item1;
        }

        public void RemoveFirst()
        {
            queue.RemoveAt(queue.Count - 1);
        }

        public int Count
        {
            get
            {
                return queue.Count;
            }
        }
        //*/
        /*
        private List<Tuple<List<IVertex>, double>> queue;

        public PriorityQueue()
        {
            queue = new List<Tuple<List<IVertex>, double>>();
        }

        public void Add(double cost, List<IVertex> elem)
        {
            int i;
            for (i = 0; i < queue.Count && cost > queue[i].Item2; i++) ;
            queue.Insert(i, new Tuple<List<IVertex>, double>(elem, cost));
        }

        public List<IVertex> First()
        {
            return queue[0].Item1;
        }

        public void RemoveFirst()
        {
            queue.RemoveAt(0);
        }

        public int Count
        {
            get
            {
                return queue.Count;
            }
        }
        */
        /*
        private Dictionary<double, List<List<IVertex>>> queue;

        public PriorityQueue()
        {
            queue = new Dictionary<double, List<List<IVertex>>>();
        }

        public void Add(double cost, List<IVertex> elem)
        {
            if (queue.ContainsKey(cost))
                queue[cost].Add(elem);
            else
            {
                var list = new List<List<IVertex>>();
                list.Add(elem);
                queue.Add(cost, list);
            }
        }

        public List<IVertex> First()
        {
            return queue[queue.Keys.Min()].Last();
        }

        public void RemoveFirst()
        {
            var list = queue[queue.Keys.Min()];
            if (list.Count > 1)
                list.RemoveAt(list.Count - 1);
            else
                queue.Remove(queue.Keys.Min());
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var x in queue.Values)
                    count += x.Count;
                return count;
            }
        }
        //*/
    }
}