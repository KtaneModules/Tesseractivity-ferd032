using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class TesseractivityScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public GameObject[] LightSphereObjs;
    public Material[] LightSphereMats;
    public TextMesh[] AxisTexts;
    public GameObject LightInfoLED;
    public GameObject HighlightedSphere;
    public KMSelectable[] Buttons;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private const int _gridSize = 4;

    private sealed class SphereCell : IEquatable<SphereCell>
    {
        public int x;
        public int y;
        public int z;
        public int w;

        public SphereCell(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public bool Equals(SphereCell other)
        {
            return other != null && other.x == x && other.y == y && other.z == z && other.w == w;
        }
    }

    private bool[][][][] _lightStates = new bool[_gridSize][][][]
    {
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        },
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        },
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        },
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        }
    };

    private bool[][][][] _toggledSpheres = new bool[_gridSize][][][]
    {
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        },
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        },
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        },
        new bool[_gridSize][][] {
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] },
            new bool[_gridSize][] { new bool[_gridSize], new bool[_gridSize], new bool[_gridSize], new bool[_gridSize] }
        }
    };

    private SphereCell _currentSphere = new SphereCell(0, 0, 0, 0);

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int btn = 0; btn < Buttons.Length; btn++)
            Buttons[btn].OnInteract += ButtonPress(btn);
        Shuffle();
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            if (btn == 4)
            {
                Audio.PlaySoundAtTransform("Toggle", transform);
                ToggleSpheres(_currentSphere);
                Debug.LogFormat("[Tesseractivity #{0}] Toggled: X{1} Y{2} Z{3} W{4}.", _moduleId, _currentSphere.x, _currentSphere.y, _currentSphere.z, _currentSphere.w);
            }
            else
            {
                switch (btn)
                {
                    case 0: _currentSphere.x = (_currentSphere.x + 1) % 4; Audio.PlaySoundAtTransform("Scroll" + _currentSphere.x.ToString(), transform); AxisTexts[btn].text = _currentSphere.x.ToString(); break;
                    case 1: _currentSphere.y = (_currentSphere.y + 1) % 4; Audio.PlaySoundAtTransform("Scroll" + _currentSphere.y.ToString(), transform); AxisTexts[btn].text = _currentSphere.y.ToString(); break;
                    case 2: _currentSphere.z = (_currentSphere.z + 1) % 4; Audio.PlaySoundAtTransform("Scroll" + _currentSphere.z.ToString(), transform); AxisTexts[btn].text = _currentSphere.z.ToString(); break;
                    case 3: _currentSphere.w = (_currentSphere.w + 1) % 4; Audio.PlaySoundAtTransform("Scroll" + _currentSphere.w.ToString(), transform); AxisTexts[btn].text = _currentSphere.w.ToString(); break;
                }
                SetSphereColors();
            }
            if (GetCount().Contains(0))
            {
                Debug.LogFormat("[Tesseractivity #{0}] All lights have been set to the same state. Module solved!", _moduleId);
                _moduleSolved = true;
                Module.HandlePass();
                Audio.PlaySoundAtTransform("Solve", transform);
                HighlightedSphere.SetActive(false);
            }
            return false;
        };
    }

    private void Shuffle()
    {
        Reshuffle:
        int rndCount = Rnd.Range(20, 40);
        for (int i = 0; i < rndCount; i++)
            ToggleSpheres(new SphereCell(Rnd.Range(0, _gridSize), Rnd.Range(0, _gridSize), Rnd.Range(0, _gridSize), Rnd.Range(0, _gridSize)));
        if (GetCount().Contains(0))
            goto Reshuffle;
        Debug.LogFormat("[Tesseractivity #{0}] Grid:", _moduleId);
        LogGrid();
    }

    private int[] GetCount()
    {
        int onCount = 0;
        int offCount = 0;
        for (int x = 0; x < _lightStates.Length; x++)
            for (int y = 0; y < _lightStates[x].Length; y++)
                for (int z = 0; z < _lightStates[x][y].Length; z++)
                    for (int w = 0; w < _lightStates[x][y][z].Length; w++)
                    {
                        if (_lightStates[x][y][z][w])
                            onCount++;
                        else
                            offCount++;
                    }
        return new[] { offCount, onCount };
    }

    private List<SphereCell> GetAdjacents(SphereCell sph)
    {
        var list = new List<SphereCell>();
        list.Add(sph);
        if (sph.x != 0)
            list.Add(new SphereCell(sph.x - 1, sph.y, sph.z, sph.w));
        if (sph.x != _gridSize - 1)
            list.Add(new SphereCell(sph.x + 1, sph.y, sph.z, sph.w));
        if (sph.y != 0)
            list.Add(new SphereCell(sph.x, sph.y - 1, sph.z, sph.w));
        if (sph.y != _gridSize - 1)
            list.Add(new SphereCell(sph.x, sph.y + 1, sph.z, sph.w));
        if (sph.z != 0)
            list.Add(new SphereCell(sph.x, sph.y, sph.z - 1, sph.w));
        if (sph.z != _gridSize - 1)
            list.Add(new SphereCell(sph.x, sph.y, sph.z + 1, sph.w));
        if (sph.w != 0)
            list.Add(new SphereCell(sph.x, sph.y, sph.z, sph.w - 1));
        if (sph.w != _gridSize - 1)
            list.Add(new SphereCell(sph.x, sph.y, sph.z, sph.w + 1));
        return list;
    }

    private void ToggleSpheres(SphereCell sph)
    {
        _toggledSpheres[sph.x][sph.y][sph.z][sph.w] = !_toggledSpheres[sph.x][sph.y][sph.z][sph.w];
        var spheres = GetAdjacents(sph);
        for (int i = 0; i < spheres.Count; i++)
            _lightStates[spheres[i].x][spheres[i].y][spheres[i].z][spheres[i].w] = !_lightStates[spheres[i].x][spheres[i].y][spheres[i].z][spheres[i].w];
        SetSphereColors();
    }

    private void SetSphereColors()
    {
        for (int x = 0; x < _lightStates.Length; x++)
            for (int y = 0; y < _lightStates[x].Length; y++)
                for (int z = 0; z < _lightStates[x][y].Length; z++)
                {
                    LightSphereObjs[z * _gridSize * _gridSize + y * _gridSize + x].GetComponent<MeshRenderer>().material = _lightStates[x][y][z][_currentSphere.w] ? LightSphereMats[1] : LightSphereMats[0];
                    if (x == _currentSphere.x && y == _currentSphere.y && z == _currentSphere.z)
                        HighlightedSphere.transform.localPosition = new Vector3(
                            LightSphereObjs[z * _gridSize * _gridSize + y * _gridSize + x].transform.localPosition.x,
                            LightSphereObjs[z * _gridSize * _gridSize + y * _gridSize + x].transform.localPosition.y,
                            LightSphereObjs[z * _gridSize * _gridSize + y * _gridSize + x].transform.localPosition.z
                            );
                }
        LightInfoLED.GetComponent<MeshRenderer>().material = _lightStates[_currentSphere.x][_currentSphere.y][_currentSphere.z][_currentSphere.w] ? LightSphereMats[1] : LightSphereMats[0];
    }

    private void LogGrid()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Debug.LogFormat("[Tesseractivity #{0}] {1} {2} {3} {4} | {5} {6} {7} {8} | {9} {10} {11} {12} | {13} {14} {15} {16}", _moduleId,
                    _lightStates[0][j][i][0] ? "#" : "*", _lightStates[1][j][i][0] ? "#" : "*", _lightStates[2][j][i][0] ? "#" : "*", _lightStates[3][j][i][0] ? "#" : "*",
                    _lightStates[0][j][i][1] ? "#" : "*", _lightStates[1][j][i][1] ? "#" : "*", _lightStates[2][j][i][1] ? "#" : "*", _lightStates[3][j][i][1] ? "#" : "*",
                    _lightStates[0][j][i][2] ? "#" : "*", _lightStates[1][j][i][2] ? "#" : "*", _lightStates[2][j][i][2] ? "#" : "*", _lightStates[3][j][i][2] ? "#" : "*",
                    _lightStates[0][j][i][3] ? "#" : "*", _lightStates[1][j][i][3] ? "#" : "*", _lightStates[2][j][i][3] ? "#" : "*", _lightStates[3][j][i][3] ? "#" : "*"
                    );
            }
            if (i != 3)
                Debug.LogFormat("[Tesseractivity #{0}] --------+---------+--------+--------", _moduleId);
        }
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = @"!{0} x/y/z/w/t [Presses the specified coordinate button, or toggle the selected cell.] | !{0} setspeed 0.1 [Set the speed between presses, by default 0.1 seconds.]";
#pragma warning restore 0414

    private float _tpSpeed = 0.1f;

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var parameters = command.ToLowerInvariant().Split();
        var m = Regex.Match(parameters[0], @"^\s*setspeed\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            if (parameters.Length != 2)
                yield break;
            float tempSpeed;
            if (!float.TryParse(parameters[1], out tempSpeed) || tempSpeed <= 0 || tempSpeed > 2)
            {
                yield return "sendtochaterror " + parameters[1] + " is not a valid speed! Press speed must be between 0 and 2 seconds.";
                yield break;
            }
            yield return null;
            _tpSpeed = tempSpeed;
            yield return "sendtochat Lights Cubed press speed has been set to " + parameters[1];
            yield break;
        }
        var chars = "xyzwt ".ToCharArray();
        var list = new List<int>();
        for (int i = 0; i < command.Length; i++)
        {
            int ix = Array.IndexOf(chars, command[i]);
            if (ix == -1)
                yield break;
            if (ix == 5)
                continue;
            list.Add(ix);
        }
        yield return null;
        for (int i = 0; i < list.Count; i++)
        {
            Buttons[list[i]].OnInteract();
            yield return new WaitForSeconds(_tpSpeed);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (_currentSphere.x != 0) { Buttons[0].OnInteract(); yield return new WaitForSeconds(0.1f); }
        while (_currentSphere.y != 0) { Buttons[1].OnInteract(); yield return new WaitForSeconds(0.1f); }
        while (_currentSphere.z != 0) { Buttons[2].OnInteract(); yield return new WaitForSeconds(0.1f); }
        while (_currentSphere.w != 0) { Buttons[3].OnInteract(); yield return new WaitForSeconds(0.1f); }
        for (int w = 0; w < 4; w++)
        {
            for (int z = 0; z < 4; z++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        if (_toggledSpheres[x][y][z][w])
                        {
                            Buttons[4].OnInteract();
                            if (_moduleSolved)
                                yield break;
                            yield return new WaitForSeconds(0.1f);
                        }
                        Buttons[0].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                    Buttons[1].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                Buttons[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            Buttons[3].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
