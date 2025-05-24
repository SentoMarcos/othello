using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Tile[] board = new Tile[Constants.NumTiles];
    public Node parent;
    public List<Node> childList = new List<Node>();
    public int type;//Constants.MIN o Constants.MAX
    public double utility;
    public double alfa;
    public double beta;
    
    // Añadimos campos para tracking
    public int movePosition; // Posición del movimiento que llevó a este nodo
    public int depth; // Profundidad del nodo en el árbol

    public Node(Tile[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            this.board[i] = new Tile();
            this.board[i].value = tiles[i].value;
        }
        this.movePosition = -1; // -1 indica nodo raíz
        this.depth = 0;
    }    
}

public class Player : MonoBehaviour
{
    public int turn;    
    private BoardManager boardManager;
    
    // Configuración del MINIMAX
    private int maxDepth = 4; // Profundidad máxima del árbol
    private int originalTurn; // Para restaurar el valor original

    void Start()
    {
        boardManager = GameObject.FindGameObjectWithTag("BoardManager").GetComponent<BoardManager>();
    }
       
    /*
     * Entrada: Dado un tablero
     * Salida: Posición donde mueve  
     */
    public int SelectTile(Tile[] board)
    {        
        // Guardamos el valor original del turn
        originalTurn = turn;
        
        Debug.Log("=== INICIANDO MINIMAX ===");
        Debug.Log("Turno actual: " + turn);
        
        // Generamos el nodo raíz del árbol (MAX - nuestro turno)
        Node root = new Node(board);
        root.type = Constants.MAX;
        root.depth = 0;
        
        // Generamos el árbol completo hasta la profundidad máxima
        GenerateTree(root, turn, 0, maxDepth);
        
        Debug.Log("Árbol generado. Nodos en nivel 1: " + root.childList.Count);
        
        // Aplicamos MINIMAX para calcular utilidades
        CalculateMinimax(root);
        
        // Seleccionamos el mejor movimiento
        int bestMove = SelectBestMove(root);
        
        // Restauramos el valor original del turn
        turn = originalTurn;
        
        Debug.Log("Mejor movimiento seleccionado: " + bestMove);
        Debug.Log("=== FIN MINIMAX ===");
        
        return bestMove;
    }
    
    /*
     * Genera el árbol MINIMAX recursivamente
     */
    private void GenerateTree(Node currentNode, int currentTurn, int currentDepth, int maxDepth)
    {
        // Caso base: hemos llegado a la profundidad máxima
        if (currentDepth >= maxDepth)
        {
            // Este es un nodo terminal, calculamos su utilidad
            currentNode.utility = CalculateUtility(currentNode.board);
            return;
        }
        
        // Obtenemos las casillas donde puede mover el jugador actual
        List<int> selectableTiles = boardManager.FindSelectableTiles(currentNode.board, currentTurn);
        
        // Si no hay movimientos posibles, el jugador pasa turno
        if (selectableTiles.Count == 0)
        {
            Debug.Log("No hay movimientos disponibles en profundidad " + currentDepth + 
                     " para jugador " + currentTurn + ". Pasando turno.");
            
            // Creamos un nodo hijo con el mismo tablero pero cambiando de jugador
            Node passNode = new Node(currentNode.board);
            passNode.parent = currentNode;
            passNode.depth = currentDepth + 1;
            passNode.movePosition = -2; // -2 indica "pasar turno"
            
            // El tipo del nodo hijo es el opuesto al padre
            passNode.type = (currentNode.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            
            currentNode.childList.Add(passNode);
            
            // Continuamos generando el árbol con el turno opuesto
            GenerateTree(passNode, -currentTurn, currentDepth + 1, maxDepth);
            return;
        }
        
        // Generamos un nodo hijo para cada movimiento posible
        foreach (int tilePosition in selectableTiles)
        {
            // Creamos un nuevo nodo hijo copiando el tablero del padre
            Node childNode = new Node(currentNode.board);
            
            // Configuramos las propiedades del nodo hijo
            childNode.parent = currentNode;
            childNode.depth = currentDepth + 1;
            childNode.movePosition = tilePosition;
            
            // El tipo del nodo hijo es el opuesto al padre
            childNode.type = (currentNode.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            
            // Aplicamos el movimiento al tablero del nodo hijo
            boardManager.Move(childNode.board, tilePosition, currentTurn);
            
            // Añadimos el nodo hijo a la lista del padre
            currentNode.childList.Add(childNode);
            
            // Continuamos generando el árbol recursivamente con el turno opuesto
            GenerateTree(childNode, -currentTurn, currentDepth + 1, maxDepth);
        }
        
        Debug.Log("Profundidad " + currentDepth + ": generados " + 
                 currentNode.childList.Count + " nodos hijos");
    }
    
    /*
     * Calcula la utilidad de un tablero dado
     * Por ahora implementamos una función simple: diferencia de fichas
     */
    private double CalculateUtility(Tile[] board)
    {
        int myPieces = boardManager.CountPieces(board, originalTurn);
        int opponentPieces = boardManager.CountPieces(board, -originalTurn);
        
        // Función de utilidad simple: diferencia de fichas
        double utility = myPieces - opponentPieces;
        
        // Bonificación por victoria completa
        if (opponentPieces == 0) utility += 1000;
        if (myPieces == 0) utility -= 1000;
        
        return utility;
    }
    
    /*
     * Aplica el algoritmo MINIMAX para calcular las utilidades
     */
    private double CalculateMinimax(Node node)
    {
        // Si es un nodo terminal (hoja), ya tiene calculada su utilidad
        if (node.childList.Count == 0)
        {
            return node.utility;
        }
        
        // Si es un nodo MAX, tomamos el valor máximo de los hijos
        if (node.type == Constants.MAX)
        {
            double maxValue = double.MinValue;
            
            foreach (Node child in node.childList)
            {
                double childValue = CalculateMinimax(child);
                if (childValue > maxValue)
                {
                    maxValue = childValue;
                }
            }
            
            node.utility = maxValue;
            return maxValue;
        }
        // Si es un nodo MIN, tomamos el valor mínimo de los hijos
        else
        {
            double minValue = double.MaxValue;
            
            foreach (Node child in node.childList)
            {
                double childValue = CalculateMinimax(child);
                if (childValue < minValue)
                {
                    minValue = childValue;
                }
            }
            
            node.utility = minValue;
            return minValue;
        }
    }
    
    /*
     * Selecciona el mejor movimiento basado en las utilidades calculadas
     */
    private int SelectBestMove(Node root)
    {
        if (root.childList.Count == 0)
        {
            Debug.LogError("¡No hay movimientos disponibles!");
            return -1;
        }
        
        Node bestChild = root.childList[0];
        double bestUtility = root.childList[0].utility;
        
        // Buscamos el hijo con la mejor utilidad
        for (int i = 1; i < root.childList.Count; i++)
        {
            Node child = root.childList[i];
            if (child.utility > bestUtility)
            {
                bestUtility = child.utility;
                bestChild = child;
            }
        }
        
        Debug.Log("Mejor utilidad encontrada: " + bestUtility);
        Debug.Log("Movimiento corresponde a posición: " + bestChild.movePosition);
        
        // Si el mejor movimiento es "pasar turno", necesitamos manejar este caso
        if (bestChild.movePosition == -2)
        {
            Debug.LogWarning("El mejor movimiento es pasar turno - esto no debería ocurrir");
            return -1;
        }
        
        return bestChild.movePosition;
    }
    
    /*
     * Función auxiliar para debug: imprime información del árbol
     */
    private void PrintTreeInfo(Node node, int level = 0)
    {
        string indent = new string(' ', level * 2);
        Debug.Log(indent + "Nivel " + level + " - Tipo: " + 
                 (node.type == Constants.MAX ? "MAX" : "MIN") + 
                 " - Utilidad: " + node.utility + 
                 " - Movimiento: " + node.movePosition + 
                 " - Hijos: " + node.childList.Count);
        
        foreach (Node child in node.childList)
        {
            PrintTreeInfo(child, level + 1);
        }
    }
}