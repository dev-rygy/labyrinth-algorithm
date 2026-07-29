# Labyrinth Algorithm

## Intro
I began building this algorithm back in 2023 for a club at my university. My peers and I were tasked with procedurally generating a 3D dungeon for an FPS we were making. Goes without saying, between coursework and other obligations, a single semester wasn't enough time to finish a game of that magnitude. . The game itself never came together, but it was enough to hook me. I picked the algorithm back up and kept developing it in 2024.

The procedural generation was simple at first, relying on an algorithm called Drunkard Walk that spawned rooms in random directions from the previous one. I later added Delaunay triangulation, A\*, and Dijkstra's algorithm to the pipeline.

Most of the algorithms the Labyrinth uses today are well known in the proc-gen community, but there's one feature I can genuinely call my own. Back in the club days, I had little formal knowledge of how procedural generation actually worked, so in a way, I had to get creative. Drawing on what I knew about Drunkard Walk and rule tiles, I came up with a feature I call "Blueprints", individual, single-celled components on a grid that combine to form an entire map.

I've always admired how games like _The Binding of Isaac_, _Enter the Gungeon_, and _Wizard of Legend_ generate their maps, but I equally admired the intricate, deliberate level design of games like _Zelda_ and _Dark Souls_. Was there a way to randomize dungeon layouts while still producing levels that felt hand-crafted and made sense? Is it possible to link these levels together seamlessly so that different themed areas didn't need to be loaded in a different scene? That's the problem I set out to solve.

The algorithm was made in Unity 3D but can be rewritten to work in any game engine.
## Blueprints
Blueprints are the backbone of the algorithm. They must exist in order for any rooms to generate. Blueprints are simply single-celled marks on a grid that tell the algorithm "a room can eventually spawn here". When the blueprints are done generating a second pass then parses the blueprints and generates rooms based on a set of rules. Think of how Rule-Tiles work in game engines.

```
public class Blueprint
{
    public readonly string CellID;
    public readonly Vector3Int Position;    // Position of blueprint coords on grid
    public bool Claimed { get; set; }       // Prevents/allows parsing algorithm to use blueprint
    public bool[] EntryPointFlags { get; set; }

    // Constructor
    public Blueprint(Vector3Int postion, string cellID = "Blueprint")
    {
        Claimed = true;
        CellID = cellID;
        Position = postion;
        EntryPointFlags = new bool[6];       // A flag to mark which entrances should be open for a room
    }
}
```

To prevent blueprints from spawning on top of other blueprints a Dictionary is used. A C# dictionary has O(1) lookup time, cannot contain duplicates, and can allocate space dynamically. To prevent rooms from spawning on top of each other blueprints house a flag called `Claimed` that tells the room parser whether or not to include the blueprint in it's check. Finally, `EntryPointFlags` tell the room what doorways it needs to open. These flags are determined during the blueprint pass of the algorithm.
## Blueprint Operations
As the algorithm grew larger, I began to worry about how hard it would be to debug in future iterations. At the time, the procedural generator executed all of its code in one go, but I wanted a way to step through each process individually, something akin to setting breakpoints in code. This is when I had the idea of applying concepts from assembly and instruction set architecture to my algorithm. Little did I know that building this debugger would end up making my procedural generator more dynamic and controllable than ever before.

To accomplish this, I needed to split each of the algorithm's processes into individual, executable components I call "Blueprint Operations." Each operation holds both input and output data, similar to registers in an assembly language. Operations are initialized and placed into a queue, then executed one by one.

```
public abstract class BlueprintOperation
{
	// DataID for is used strictly for debugging purposes.
	public string OperationID { get; protected set; }

	// Ports are the auguments and return values of operations.
	public List<string> InputPorts { get; protected set; }
	public List<string> OutputPorts { get; protected set; }

	protected MapGenerationContext _context;

	public BlueprintOperation(MapGenerationContext context)
	{
		InputPorts = new List<string>();
		OutputPorts = new List<string>();

		_context = context;
	}

	...
}

...

public sealed class MapGenerationContext
{
	// Quick random access blueprint container; stores all blueprints generated in map
	private static Dictionary<Vector3Int, Blueprint> _blueprintDictionary;
	public static Dictionary<Vector3Int, Blueprint> BlueprintDictionary => _blueprintDictionary;

	...
}
```

Just like in assembly, the outputs of some operations need to be stored in memory so other operations can use them later. The map generation's `context` holds both the instruction/operation execution order and the data used in those operations.

```
public sealed class MapGenerationContext
{
	...

    public int OperationIDCounter { get; private set; } = 10000;
    public int MemoryIDCounter { get; private set; } = 20000;

	// Stores blueprint operations to be executed later
    public LinkedList<BlueprintOperation> OperationQueue { get; private set; }

	// Stores outputs from blueprint operations
    private Dictionary<string, object> _dataCache;

    public MapGenerationContext()
    {
        _dataCache = new();
        _blueprintDictionary = new();
        OperationQueue = new();
    }

    public BlueprintOperation OperationQueuePeek()
    {
        return OperationQueue?.First.Value;
    }

    public void OperationQueueEnqueue(BlueprintOperation op)
    {
        OperationQueue?.AddLast(op);

    }

    public void OperationQueueAddFront(BlueprintOperation op)
    {
        OperationQueue?.AddFirst(op);
    }

    public BlueprintOperation OperationQueueDequeue()
    {
        // Check to see if queue has atleast one operation stored
        if (OperationQueue?.First is null)
            return null;

        BlueprintOperation op = OperationQueue?.First.Value;
        OperationQueue?.RemoveFirst();
        return op;
    }
    
    // Get data
    public bool TryGet<T>(string memoryID, out T value)
    {
        if (_dataCache is null)
        {
            value = default;
            return false;
        }

        if (_dataCache.TryGetValue(memoryID, out object obj) && obj is T castValue)
        {
            value = castValue;
            return true;
        }

        value = default;
        return false;
    }

	// Store data
    public void Malloc(string memoryID, object value)
    {
        if (_dataCache is null)
            return;

        _dataCache[memoryID] = value;
    }

	// Check for data
    public bool Contains(string memoryID)
    {
        if (_dataCache is null)
        {
            Debug.LogError($"Memory object not set.");
            return false;
        }

        return _dataCache.ContainsKey(memoryID);
    }

	// Remove data
    public void Remove(string memoryID)
    {
        if (_dataCache is null)
        {
            Debug.LogError($"Memory object not set.");
            return;
        }

        if (_dataCache.ContainsKey(memoryID))
            _dataCache.Remove(memoryID);
    }

    public int ConsumeOperationID()
    {
        return OperationIDCounter++;
    }

    public int ConsumeMemoryID()
    {
        return MemoryIDCounter++;
    }
    
    ...
}
```
## Blueprint Graph
Around the time I was implementing this feature, I started learning a bit of Unreal Engine and was impressed by its visual scripting graph, conveniently called "Blueprints." That's when I came upon a revelation. How different is my way of executing Blueprint Operations from Unreal's way of executing code, really? Each operation is essentially a node with inputs, outputs, and an execution order. They can be executed in any order and support a wide range of use cases. For example, this implementation lets me require a key from a separate path to unlock a door to a new one, or pathfind to any given room using any heuristic of my choosing. This is great for hidden secrets, connecting zones, and countless other unique cases.

![[Pasted image 20260729133359.png]]

The graph contains many intermediary operations that are also useful to the developer. Set operations like union, intersection, and difference can be applied to blueprints for interesting generations. There are random access operations for variation, and even branch and jump operations for loops.

Right now, all nodes are hardcoded into a controller, though I'd like to move to Unity's graphing API in a future implementation. Below is the controller used to both store and execute operations. Operations can be stepped through by a specified `stepLength`, given via the `Advance()` function.

```
public class MapGenerationController
{
	private void LoadOperations()
	{
		// Hardcoded operations listed in their order of execution. Loaded into context to be executed later.
		
		...
	}

	private void ConsumeStep()
	{	
		if (_runToEnd)
			return;
		
		if (_stepBudget > 0)
			_stepBudget--;
	}
	
	public void Advance(int stepLength)
	{	
		if (stepLength <= 0)
			return;
		
		_stepBudget += stepLength;
		_runToEnd = false;
	}
	
	public void AdvanceAll()
	{
		if (!IsGenerating)
			return;
		
		_runToEnd = true;
	}
	
	private IEnumerator ExecuteOperations()
	{
		// Execute operations and generate blueprints
		while (_context.OperationQueue.Count > 0)
		{
			// Halt the execution of operations
			while (!_runToEnd && _stepBudget <= 0)
				yield return null;
			
			ConsumeStep();
		
			// Dequeue the current opration
			BlueprintOperation operation = _context.OperationQueueDequeue();
			if (operation == null)
				throw new ArgumentNullException(nameof(operation));
		
			// Execute Operation
			bool result = operation.Execute();
		
			// Operation failed to execute; stop running coroutine
			if (!result)
			{
				GenerationFailed();
				yield break;
			}
		}
	}
	
	...
}
```

## Zones
One of the most important problems I set out to solve was a way to generate themed areas and connect them together in a seamless fashion. As stated in the intro, alongside rogue-likes I'm also a huge fan of RPGs that can transport the player to different areas from multiple entry points on the map. The concept of "Zones" helped me accomplish this.

Zones connect to one another through intermediary zones I call "Connection Zones." These zones simply intersect their bounds with others and connect them via their own blueprint operations.

As of now, Zones are purely containers that house unique rooms, branches, and bounds that contain their blueprints. A controller still determines zone generation, though in a future update, Zones will be able to house their own operations and blueprint graph for unique generation rules.

## Unique Blueprint Placement
Unique rooms are rooms determined by their zone before runtime; the blueprints generated in place of these rooms _are not_ to be included later in the room parsing procedure. These are most likely boss rooms, mini-boss rooms, large rooms, or any rooms whose blueprint shape would be difficult or impossible to parse later.

These rooms can either be fixed (placed in a set position) or bounded (placed randomly within set bounds). Both cases have their respective blueprint operations. The blueprints generated from this operation can later be used as points for the Delaunay and A\* operations.
## Divergent Blueprint Placement
Some zones may have few unique rooms, which means a small set of points for pathfinding. The resulting generation would be very simple and boring. To make the generation a bit more windy and organic, we place what I call "Divergent Rooms" to add more points to the pathfinding graph.

Divergent rooms are a set number of blueprints randomly spawned within a zone. These blueprints can have varied dimensions, described as an input to the operation. The blueprints generated from this operation can later be used as points by the Delaunay and A\* operations.
## Delaunay Triangulation
Delaunay Triangulation takes a set of defined points and creates a triangle mesh that can later be used as a graph for pathfinding. In 2D, it's fairly simple, but in 3D it gets complicated, instead of triangles we must use tetrahedra and calculate the volume of those tetrahedra using the determinant function.

Delaunay uses a set of blueprint positions as points and creates edges connecting them. It also helps prevent the generation of a dungeon with too many long and unnecessary paths between rooms.

A major problem I faced was dealing with degenerate tetrahedra, which can occur when all four points are coplanar. I needed to ensure that at least one blueprint point used in a tetrahedron exists on a different floor than the other three, to prevent this from happening. When a zone is completely flat (one floor), I can just use 2D Delaunay to connect the blueprints instead.

Delaunay ensures every point in the resultant graph is connected, which means no room is left out. However, this can still result in too many edges, leading to an excess of branches and rooms. To manage this, I use a greedy algorithm, Prim's Algorithm, to find the MST (minimum spanning tree) in the graph. From there, we can randomly select other edges in the graph if we want some loops in our dungeon.
## Pathfinding
Many pathfinding algorithms exist that could help connect blueprints together, but none are as fast and versatile as A\*. Using a pathfinding operation, I can either connect two blueprints directly or pass in a graph and connect all of its nodes. A\* returns all the positions needed to connect the blueprints, and from there I simply spawn new blueprints in their place.

A cost/heuristic function tells A\* how to path toward each blueprint. The heuristic can make the path curvy, straight, or zig-zagged. There are endless possibilities for what the cost function can be, but as of now I use four well-known functions:

1. Euclidean: creates a fairly straight path to the target.
	$$D(a, b) = sqrt[ (x_2 - x_1)^2 + (y_2 - y_1)^2 + (z_2 - z_1)^2 ]$$

2. Manhattan: causes the path to stay parallel to the x, y, and z axes.
	$$D(a, b) = | x_2 - x_1 | + | y_2 - y_1 | + | z_2 - z_1 |$$
	
3. Chebyshev: causes the path to zig-zag and staircase.
	$$D(a, b) = max[ | x_2 - x_1 |, | y_2 - y_1 |, | z_2 - z_1 | ]$$

4. Dijkstra: guaranteed optimal path; cheapest route.
	$$D(a, b) = 0$$
