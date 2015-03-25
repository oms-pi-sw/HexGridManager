﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Path<Node>: IEnumerable<Node>
{
	public Node LastStep { get; private set; }
	public Path<Node> PreviousSteps { get; private set; }
	public double TotalCost { get; private set; }
	private Path(Node lastStep, Path<Node> previousSteps, double totalCost)
	{
		LastStep = lastStep;
		PreviousSteps = previousSteps;
		TotalCost = totalCost;
	}
	public Path(Node start) : this(start, null, 0) {}
	public Path<Node> AddStep(Node step, double stepCost)
	{
		return new Path<Node>(step, this, TotalCost + stepCost);
	}
	public IEnumerator<Node> GetEnumerator()
	{
		for (Path<Node> p = this; p != null; p = p.PreviousSteps)
			yield return p.LastStep;
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}


class PriorityQueue<P, V>
{
	private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();
	public void Enqueue(P priority, V value)
	{
		Queue<V> q;
		if (!list.TryGetValue(priority, out q))
		{
			q = new Queue<V>();
			list.Add(priority, q);
		}
		q.Enqueue(value);
	}
	public V Dequeue()
	{
		// will throw if there isn’t any first element!
		var pair = list.First();
		var v = pair.Value.Dequeue();
		if (pair.Value.Count == 0) // nothing left of the top priority.
			list.Remove(pair.Key);
		return v;
	}
	public bool IsEmpty
	{
		get { return !list.Any(); }
	}
}

public static class PathFinder
{
	//distance f-ion should return distance between two adjacent nodes
	//estimate should return distance between any node and destination node
	public static Path<Node> FindPath<Node>(Node start,	Node destination, Func<Node, Node, double> distance, Func<Node, double> estimate)
		where Node: IHasNeighbours<Node>
	{
		//set of already checked nodes
		var closed = new HashSet<Node>();
		//queued nodes in open set
		var queue = new PriorityQueue<double, Path<Node>>();
		queue.Enqueue(0, new Path<Node>(start));
		
		while (!queue.IsEmpty)
		{
			var path = queue.Dequeue();
			
			if (closed.Contains(path.LastStep))
				continue;
			if (path.LastStep.Equals(destination))
				return path;
			
			closed.Add(path.LastStep);
			
			foreach (Node n in path.LastStep.Neighbours)
			{
				double d = distance(path.LastStep, n);
				//new step added without modifying current path
				var newPath = path.AddStep(n, d);
				queue.Enqueue(newPath.TotalCost + estimate(n), newPath);
			}
		}
		
		return null;
	}
}