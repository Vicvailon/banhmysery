using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    [SerializeField] private GameObject _testGhost;
    [SerializeField] private int _numberOfTrails = 3;
    [SerializeField] private float _fadeTime = 1f;

    public SpriteRenderer[] PlayerSpriteRenderers;

    private Vector3[] _positions;
    private Quaternion[] _rotations;
    private Vector3[] _scales;

    private GameObject[] _ghosts;

    private int _matAlpha = Shader.PropertyToID("_GhostAlpha");

    private void Awake()
    {
        _positions = new Vector3[PlayerSpriteRenderers.Length];
        _rotations = new Quaternion[PlayerSpriteRenderers.Length];
        _scales = new Vector3[PlayerSpriteRenderers.Length];

        _ghosts = new GameObject[_numberOfTrails];
    }

    public void LeaveGhostTrail(float time)
    {
        StartCoroutine(LeaveTrail(time));
    }

    private IEnumerator LeaveTrail(float time)
    {
        int numberSpawned = 0;
        while (numberSpawned < _numberOfTrails)
        {
            for (int i = 0; i < _numberOfTrails; i++)
            {
                Spawn();
                yield return new WaitForSeconds(time / _numberOfTrails);
                numberSpawned++;
            }
        }
    }

    private void Spawn()
    {
        for (int i = 0; i < PlayerSpriteRenderers.Length; i++)
        {
            _positions[i] = PlayerSpriteRenderers[i].transform.position;
            _rotations[i] = PlayerSpriteRenderers[i].transform.rotation;
            _scales[i] = PlayerSpriteRenderers[i].transform.localScale;
        }

        GameObject go = Instantiate(_testGhost, transform.position, Quaternion.identity);
        go.SetActive(false);

        SpriteRenderer[] rends = go.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].transform.position = _positions[i];
            rends[i].transform.rotation = _rotations[i];
            rends[i].transform.localScale = _scales[i];
        }

        go.SetActive(true);

        StartCoroutine(FadeGhost(rends, go));
    }

    private IEnumerator FadeGhost(SpriteRenderer[] rends, GameObject go)
    {
        float elapsedTime = 0f;
        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            for (int i = 0; i < rends.Length; i++)
            {
                float newValue = Mathf.Lerp(1f, 0f, (elapsedTime / _fadeTime));
                rends[i].material.SetFloat(_matAlpha, newValue);
            }

            yield return null;
        }

        Destroy(go);
    }
}
