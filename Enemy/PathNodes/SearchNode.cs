using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SearchNode : BaseNode
{
    static private SearchNode[] _allSearchNodes;
    static public SearchNode[] allSearchNodes
    {
        get { return _allSearchNodes; }
    }

    [Tooltip("The room the search node is in.")]
    [SerializeField]
    private Room.Names _roomName;
    public Room.Names roomName
    {
        get { return _roomName; }
    }

    [Tooltip("The floor the search node is on.")]
    [SerializeField]
    private int _floor;
    public int floor
    {
        get { return _floor; }
    }

    [Tooltip("The interactable environment the search node is associated with. Leave null if there is none.")]
    [SerializeField]
    private InteractableEnvironment _environment;
    public InteractableEnvironment environment
    {
        get { return _environment; }
    }

    private bool _cleared;
    private bool _beingSearched;

    void Start()
    {
        Name = Type.Search;
        _cleared = false;
        _beingSearched = false;
    }

    public bool IsSearchable()
    {
        if (!_beingSearched && !_cleared)
            return true;
        return false;
    }

    public void StartSearch()
    {
        _beingSearched = true;
    }

    public void Cleared()
    {
        _cleared = true;
    }

    public void HardReset()
    {
        _beingSearched = false;
        _cleared = false;
    }

    public void SoftReset()
    {
        if (_cleared)
        {
            _beingSearched = false;
            _cleared = false;
        }
    }

    void OnDestroy()
    {
        if (_allSearchNodes != null && !_allSearchNodes.Contains(null))
            Array.Clear(_allSearchNodes, 0, _allSearchNodes.Length);
    }

    public static void FindAllSearchNodes()
    {
        _allSearchNodes = GameObject.FindObjectsOfType<SearchNode>();
    }
}