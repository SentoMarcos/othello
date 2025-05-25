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
    
    // Campos adicionales para tracking
    public int movePosition; // Posición del movimiento que llevó a este nodo
    public int depth; // Profundidad del nodo en el árbol
    public bool isPruned; // Indica si este nodo fue podado

    public Node(Tile[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            this.board[i] = new Tile();
            this.board[i].value = tiles[i].value;
        }
        this.movePosition = -1; // -1 indica nodo raíz
        this.depth = 0;
        this.isPruned = false;
        
        // Inicialización de alfa y beta
        this.alfa = double.MinValue;
        this.beta = double.MaxValue;
    }    
}

public class Player : MonoBehaviour
{
    public int turn;    
    private BoardManager boardManager;
    
    // Configuración del MINIMAX
    private int maxDepth = 4; // Profundidad máxima del árbol
    private int originalTurn; // Para restaurar el valor original
    
    // Estadísticas de poda
    private int totalNodes = 0;
    private int prunedNodes = 0;

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
        
        // Reiniciamos estadísticas
        totalNodes = 0;
        prunedNodes = 0;
        
        Debug.Log("=== INICIANDO MINIMAX CON PODA ALFA-BETA ===");
        Debug.Log("Turno actual: " + turn + ", Profundidad máxima: " + maxDepth);
        
        // Generamos el nodo raíz del árbol (MAX - nuestro turno)
        Node root = new Node(board);
        root.type = Constants.MAX;
        root.depth = 0;
        root.alfa = double.MinValue;
        root.beta = double.MaxValue;
        
        // Aplicamos MINIMAX con poda alfa-beta
        double bestUtility = MinimaxAlphaBeta(root, turn, 0, double.MinValue, double.MaxValue);
        
        // Seleccionamos el mejor movimiento
        int bestMove = SelectBestMove(root);
        
        // Restauramos el valor original del turn
        turn = originalTurn;
        
        Debug.Log("=== RESULTADOS MINIMAX ===");
        Debug.Log("Mejor utilidad: " + bestUtility);
        Debug.Log("Mejor movimiento: " + bestMove);
        Debug.Log("Nodos totales generados: " + totalNodes);
        Debug.Log("Nodos podados: " + prunedNodes);
        Debug.Log("Eficiencia de poda: " + (prunedNodes * 100.0 / totalNodes).ToString("F1") + "%");
        Debug.Log("=== FIN MINIMAX ===");
        
        return bestMove;
    }
    
    /*
     * Algoritmo MINIMAX con poda alfa-beta
     */
    private double MinimaxAlphaBeta(Node currentNode, int currentTurn, int currentDepth, double alpha, double beta)
    {
        totalNodes++;
        
        // Caso base: hemos llegado a la profundidad máxima
        if (currentDepth >= maxDepth)
        {
            currentNode.utility = CalculateUtility(currentNode.board);
            return currentNode.utility;
        }
        
        // Obtenemos las casillas donde puede mover el jugador actual
        List<int> selectableTiles = boardManager.FindSelectableTiles(currentNode.board, currentTurn);
        
        // Si no hay movimientos posibles, el jugador pasa turno
        if (selectableTiles.Count == 0)
        {
            // Creamos un nodo "pass" y continuamos con el oponente
            Node passNode = new Node(currentNode.board);
            passNode.parent = currentNode;
            passNode.depth = currentDepth + 1;
            passNode.movePosition = -2; // -2 indica "pasar turno"
            passNode.type = (currentNode.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
            
            currentNode.childList.Add(passNode);
            
            // Recursión con turno opuesto
            return MinimaxAlphaBeta(passNode, -currentTurn, currentDepth + 1, alpha, beta);
        }
        
        // Variables para el algoritmo
        double bestValue;
        
        if (currentNode.type == Constants.MAX)
        {
            // Nodo MAX: buscamos maximizar
            bestValue = double.MinValue;
            
            foreach (int tilePosition in selectableTiles)
            {
                // Creamos nodo hijo
                Node childNode = CreateChildNode(currentNode, tilePosition, currentTurn, currentDepth);
                
                // Llamada recursiva
                double childValue = MinimaxAlphaBeta(childNode, -currentTurn, currentDepth + 1, alpha, beta);
                
                // Actualizamos el mejor valor
                bestValue = System.Math.Max(bestValue, childValue);
                alpha = System.Math.Max(alpha, bestValue);
                
                // PODA ALFA-BETA: Si β ≤ α, podamos
                if (beta <= alpha)
                {
                    prunedNodes++;
                    childNode.isPruned = true;
                    Debug.Log("PODA en MAX - Profundidad " + currentDepth + 
                             " (α=" + alpha.ToString("F1") + ", β=" + beta.ToString("F1") + ")");
                    break; // Poda: no exploramos más hermanos
                }
            }
        }
        else
        {
            // Nodo MIN: buscamos minimizar
            bestValue = double.MaxValue;
            
            foreach (int tilePosition in selectableTiles)
            {
                // Creamos nodo hijo
                Node childNode = CreateChildNode(currentNode, tilePosition, currentTurn, currentDepth);
                
                // Llamada recursiva
                double childValue = MinimaxAlphaBeta(childNode, -currentTurn, currentDepth + 1, alpha, beta);
                
                // Actualizamos el mejor valor
                bestValue = System.Math.Min(bestValue, childValue);
                beta = System.Math.Min(beta, bestValue);
                
                // PODA ALFA-BETA: Si β ≤ α, podamos
                if (beta <= alpha)
                {
                    prunedNodes++;
                    childNode.isPruned = true;
                    Debug.Log("PODA en MIN - Profundidad " + currentDepth + 
                             " (α=" + alpha.ToString("F1") + ", β=" + beta.ToString("F1") + ")");
                    break; // Poda: no exploramos más hermanos
                }
            }
        }
        
        // Guardamos los valores alfa y beta en el nodo
        currentNode.alfa = alpha;
        currentNode.beta = beta;
        currentNode.utility = bestValue;
        
        return bestValue;
    }
    
    /*
     * Crea un nodo hijo aplicando un movimiento
     */
    private Node CreateChildNode(Node parent, int tilePosition, int currentTurn, int currentDepth)
    {
        // Creamos un nuevo nodo hijo copiando el tablero del padre
        Node childNode = new Node(parent.board);
        
        // Configuramos las propiedades del nodo hijo
        childNode.parent = parent;
        childNode.depth = currentDepth + 1;
        childNode.movePosition = tilePosition;
        childNode.type = (parent.type == Constants.MAX) ? Constants.MIN : Constants.MAX;
        
        // Aplicamos el movimiento al tablero del nodo hijo
        boardManager.Move(childNode.board, tilePosition, currentTurn);
        
        // Añadimos el nodo hijo a la lista del padre
        parent.childList.Add(childNode);
        
        return childNode;
    }
    
    /*
     * Función de utilidad mejorada
     */
    private double CalculateUtility(Tile[] board)
    {
        int myPieces = boardManager.CountPieces(board, originalTurn);
        int opponentPieces = boardManager.CountPieces(board, -originalTurn);
        
        // Componente 1: Diferencia de fichas (peso base)
        double pieceDifference = myPieces - opponentPieces;
        
        // Componente 2: Bonificación por esquinas (muy importantes)
        double cornerBonus = 0;
        int[] corners = {0, 7, 56, 63}; // Esquinas del tablero 8x8
        foreach (int corner in corners)
        {
            if (board[corner].value == originalTurn) cornerBonus += 25;
            else if (board[corner].value == -originalTurn) cornerBonus -= 25;
        }
        
        // Componente 3: Bonificación por bordes
        double edgeBonus = 0;
        for (int i = 0; i < 64; i++)
        {
            if (IsEdgePosition(i))
            {
                if (board[i].value == originalTurn) edgeBonus += 5;
                else if (board[i].value == -originalTurn) edgeBonus -= 5;
            }
        }
        
        // Componente 4: Movilidad (número de movimientos disponibles)
        int myMobility = boardManager.FindSelectableTiles(board, originalTurn).Count;
        int opponentMobility = boardManager.FindSelectableTiles(board, -originalTurn).Count;
        double mobilityBonus = (myMobility - opponentMobility) * 2;
        
        // Componente 5: Casos especiales (victoria/derrota)
        double gameEndBonus = 0;
        if (opponentPieces == 0) gameEndBonus = 1000; // Victoria total
        else if (myPieces == 0) gameEndBonus = -1000; // Derrota total
        else if (myPieces + opponentPieces == 64) // Tablero lleno
        {
            if (myPieces > opponentPieces) gameEndBonus = 500; // Victoria por mayoría
            else if (myPieces < opponentPieces) gameEndBonus = -500; // Derrota por mayoría
        }
        
        // Utilidad total combinada
        double totalUtility = pieceDifference + cornerBonus + edgeBonus + mobilityBonus + gameEndBonus;
        
        return totalUtility;
    }
    
    /*
     * Determina si una posición está en el borde del tablero
     */
    private bool IsEdgePosition(int position)
    {
        int row = position / 8;
        int col = position % 8;
        return (row == 0 || row == 7 || col == 0 || col == 7);
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
        
        Node bestChild = null;
        double bestUtility = double.MinValue;
        
        // Buscamos el hijo con la mejor utilidad
        foreach (Node child in root.childList)
        {
            if (child.utility > bestUtility && !child.isPruned)
            {
                bestUtility = child.utility;
                bestChild = child;
            }
        }
        
        // Si no encontramos ningún hijo válido, tomamos el primero
        if (bestChild == null)
        {
            bestChild = root.childList[0];
            bestUtility = bestChild.utility;
        }
        
        Debug.Log("Mejor hijo encontrado - Utilidad: " + bestUtility + 
                 ", Movimiento: " + bestChild.movePosition + 
                 ", Podado: " + bestChild.isPruned);
        
        // Si el mejor movimiento es "pasar turno", manejamos este caso
        if (bestChild.movePosition == -2)
        {
            Debug.LogWarning("El mejor movimiento es pasar turno - buscando alternativa");
            // En este caso, deberíamos buscar el primer movimiento válido
            List<int> validMoves = boardManager.FindSelectableTiles(root.board, originalTurn);
            if (validMoves.Count > 0)
            {
                return validMoves[0];
            }
        }
        
        return bestChild.movePosition;
    }
    
    /*
     * Función auxiliar para debug: imprime información del árbol con poda
     */
    private void PrintTreeInfoWithPruning(Node node, int level = 0)
    {
        string indent = new string(' ', level * 2);
        string prunedText = node.isPruned ? " [PODADO]" : "";
        
        Debug.Log(indent + "Nivel " + level + 
                 " - Tipo: " + (node.type == Constants.MAX ? "MAX" : "MIN") + 
                 " - Utilidad: " + node.utility.ToString("F1") + 
                 " - α: " + node.alfa.ToString("F1") + 
                 " - β: " + node.beta.ToString("F1") + 
                 " - Movimiento: " + node.movePosition + 
                 " - Hijos: " + node.childList.Count + prunedText);
        
        foreach (Node child in node.childList)
        {
            PrintTreeInfoWithPruning(child, level + 1);
        }
    }
}