using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

// ==========================================================
// PROYECTO: BARAJA DE CARTAS - SISTEMA INTERACTIVO
// ESTUDIANTE: [PON AQUÍ TU NOMBRE COMPLETO]
// ==========================================================

public class HttpTest : MonoBehaviour
{
    // --- MODELOS DE DATOS ÚNICOS PARA EVITAR AMBIGÜEDAD ---
    [System.Serializable]
    public class PlayerData {
        public int id;
        public string name;
        public int[] cards;
    }

    [System.Serializable]
    public class PlayerWrapper {
        public List<PlayerData> players;
    }

    [System.Serializable]
    public class CharacterCard {
        public int id;
        public string name;
        public string species;
        public string image;
    }

    [System.Serializable]
    public class RMWrapper {
        public List<CharacterCard> results;
    }

    // --- INTERFAZ GRÁFICA ---
    [Header("Marcado de Proyecto")]
    public TMP_Text uiNombreEstudianteTMP;
    public string nombreCompleto = "TU NOMBRE COMPLETO AQUÍ";

    [Header("UI Jugador")]
    public TMP_Text uiNombreJugadorTMP;
    public TMP_Text uiEstadoCargaTMP;
    public Transform contenedorCartas;
    public GameObject prefabCarta;

    [Header("Navegación")]
    public Button btnSiguiente;
    public Button btnAnterior;

    // --- VARIABLES INTERNAS ---
    private List<PlayerData> _jugadores;
    private int _indexActual = 0;
    private const string URL_PLAYERS = "https://my-json-server.typicode.com/JorgeLuisSuarique/ApiFalsaRepositorio/players";
    private const string URL_RICKMORTY = "https://rickandmortyapi.com/api/character/";

    void Start()
    {
        // inicializa nombre de proyecto y botones; arranca la carga de jugadores
        SetProjectName(nombreCompleto);
        if (btnSiguiente != null) btnSiguiente.onClick.AddListener(() => CambiarUsuario(1));
        if (btnAnterior != null)  btnAnterior.onClick.AddListener(() => CambiarUsuario(-1));
        StartCoroutine(GetPlayers());
    }

    // --------- Helpers para TMP ----------
    void SetProjectName(string text)
    {
        if (uiNombreEstudianteTMP != null) uiNombreEstudianteTMP.text = text;
    }

    void SetPlayerName(string text)
    {
        if (uiNombreJugadorTMP != null) uiNombreJugadorTMP.text = text;
    }

    void SetLoadingText(string text)
    {
        if (uiEstadoCargaTMP != null) uiEstadoCargaTMP.text = text;
    }

    IEnumerator GetPlayers()
    {
        if (uiEstadoCargaTMP != null) SetLoadingText("Cargando jugadores...");
        using (UnityWebRequest request = UnityWebRequest.Get(URL_PLAYERS))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Formateo manual para que JsonUtility acepte el array de la API Falsa
                string json = "{\"players\":" + request.downloadHandler.text + "}";
                _jugadores = JsonUtility.FromJson<PlayerWrapper>(json).players;
                ActualizarVista();
            }
            else
            {
                if (uiEstadoCargaTMP != null) SetLoadingText("Error cargando jugadores");
            }
        }
    }

    void ActualizarVista()
    {
        if (_jugadores == null || _jugadores.Count == 0) return;

        PlayerData actual = _jugadores[_indexActual];
        SetPlayerName("Jugador: " + actual.name);

        // Limpiar cartas viejas
        if (contenedorCartas != null)
        {
            foreach (Transform child in contenedorCartas) Destroy(child.gameObject);
        }

        StartCoroutine(GetCards(actual.cards));
    }

    IEnumerator GetCards(int[] ids)
    {
        if (uiEstadoCargaTMP != null) SetLoadingText("Sincronizando baraja...");
        string query = string.Join(",", ids);

        using (UnityWebRequest request = UnityWebRequest.Get(URL_RICKMORTY + query))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (uiEstadoCargaTMP != null) uiEstadoCargaTMP.text = "";
                List<CharacterCard> lista = new List<CharacterCard>();

                // Si es un solo ID, la API no devuelve un array, por eso validamos
                if (ids.Length > 1) {
                    string wrapped = "{\"results\":" + request.downloadHandler.text + "}";
                    lista = JsonUtility.FromJson<RMWrapper>(wrapped).results;
                } else {
                    lista.Add(JsonUtility.FromJson<CharacterCard>(request.downloadHandler.text));
                }

                foreach (var card in lista) CrearCartaUI(card);
            }
            else
            {
                if (uiEstadoCargaTMP != null) SetLoadingText("Error cargando cartas");
            }
        }
    }

    void CrearCartaUI(CharacterCard data)
    {
        if (prefabCarta == null || contenedorCartas == null) return;

        GameObject go = Instantiate(prefabCarta, contenedorCartas);

        // Busca el componente TMP_Text para el nombre
        TMP_Text tTMP = go.GetComponentInChildren<TMP_Text>();
        if (tTMP != null) tTMP.text = data.name;

        // Busca el componente Image para la foto
        Image img = go.GetComponentInChildren<Image>();
        if (img) StartCoroutine(DescargarFoto(data.image, img));
    }

    IEnumerator DescargarFoto(string url, Image target)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                target.sprite = Sprite.Create(tex, new Rect(0,0,tex.width, tex.height), new Vector2(0.5f,0.5f));
            }
        }
    }

    // Método público con parámetro int (visible en el Inspector)
    public void CambiarUsuario(int delta)
    {
        if (_jugadores == null || _jugadores.Count == 0) return;
        _indexActual = (_indexActual + delta) % _jugadores.Count;
        if (_indexActual < 0) _indexActual += _jugadores.Count;
        ActualizarVista();
    }

    // Alternativas sin parámetro (útiles si prefieres asignarlas desde el Inspector sin pasar int)
    public void Siguiente() => CambiarUsuario(1);
    public void Anterior()  => CambiarUsuario(-1);
}