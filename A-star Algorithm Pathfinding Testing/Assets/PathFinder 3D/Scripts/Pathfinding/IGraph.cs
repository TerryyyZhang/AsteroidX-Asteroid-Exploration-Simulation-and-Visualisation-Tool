using System.Collections.Generic;

namespace PathFinder3D
{
    public interface IGraph
    {
        // Список вершин, смежных с данной
        List<IVertex> Adjacent(IVertex vertex);
        // Стоимость пути между смежными вершинами
        double Cost(IVertex a, IVertex b);
    } 
}