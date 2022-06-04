using System.Collections.Generic;
using UnityEngine;
using Core;

public class Fragmentator : MonoBehaviour
{
    public int CellCount => (_maxSquareX + 1) * (_maxSquareY + 1);
    public float AveragePerCell => (float)_particles.Count / CellCount;

    public List<Particle> Particles
	{
        get => _particles;
        set => _particles = value;
	}
    public FastList<Particle>[,] RegionMap => _regionMap;
    public int XSquareOffset => _xSquareOffset;
    public int YSquareOffset => _ySquareOffset;
    public int MaxSquareX => _maxSquareX;
    public int MaxSquareY => _maxSquareY;
    public float ConnectionDistance => _connectionDistance;
    public Viewport Viewport
    {
        get => _viewport;
        set
		{
            _viewport = value;
            _viewport.CameraDimensionsChanged += DoFragmentation;
            DoFragmentation();
        }
    }

    public void SetConnectionDistance(float value)
	{
        _connectionDistance = value;

        DoFragmentation();
    }

    private void DoFragmentation()
	{
        if (_connectionDistance <= 0) return;
        int xSquareOffset = Mathf.FloorToInt(Viewport.MaxX / _connectionDistance) + 1;
        int ySquareOffset = Mathf.FloorToInt(Viewport.MaxY / _connectionDistance) + 1;

        if (_xSquareOffset == xSquareOffset && _ySquareOffset == ySquareOffset) return;

        _xSquareOffset = xSquareOffset;
        _ySquareOffset = ySquareOffset;
        _maxSquareX = _xSquareOffset * 2 - 1;
        _maxSquareY = _ySquareOffset * 2 - 1;
        _regionMap = new FastList<Particle>[_maxSquareX + 1, _maxSquareY + 1];

        for (int i = 0; i <= _maxSquareX; i++)
            for (int j = 0; j <= _maxSquareY; j++)
                _regionMap[i, j] = new FastList<Particle>(Mathf.CeilToInt(2 * AveragePerCell));
    }

    private float _connectionDistance;
    private List<Particle> _particles;
    private FastList<Particle>[,] _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;
    private int _maxSquareX;
    private int _maxSquareY;
    private Viewport _viewport;

	private void Update()
    {
        for (int ry = 0; ry <= _maxSquareY; ry++)
            for (int rx = 0; rx <= _maxSquareX; rx++)
                _regionMap[rx, ry].PseudoClear();

        for (int i = 0, count = _particles.Count; i < count; i++)
        {
            Particle p = _particles[i];
            GetSquare(p.Position, out int x, out int y);
            _regionMap[x, y].Add(p);
        }
    }

    private void GetSquare(Vector3 location, out int sqrX, out int sqrY)
    {
        int xp = (int)System.Math.Floor(location.x / _connectionDistance) + _xSquareOffset;
        int yp = (int)System.Math.Floor(location.y / _connectionDistance) + _ySquareOffset;
        sqrX = Mathf.Clamp(xp, 0, _maxSquareX);
        sqrY = Mathf.Clamp(yp, 0, _maxSquareY);
    }
}
