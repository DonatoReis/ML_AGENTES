using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialChao : MonoBehaviour
{
    [Header("Configurações da Textura")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private int tilesCount = 4;
    [SerializeField] private Color tileColor = new Color(0.15f, 0.15f, 0.15f); // Cinza escuro
    [SerializeField] private Color lineColor = new Color(0.3f, 0.3f, 0.3f);    // Cinza claro para as linhas
    [SerializeField] private float lineWidth = 2f;

    private void Start()
    {
        GenerateAndApplyMaterial();
    }

    private void GenerateAndApplyMaterial()
    {
        // Criar a textura
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Bilinear
        };

        float tileSize = textureSize / tilesCount;

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float xPos = x % tileSize;
                float yPos = y % tileSize;

                // Determinar se o pixel faz parte da linha
                bool isLine = xPos < lineWidth || 
                            xPos > tileSize - lineWidth || 
                            yPos < lineWidth || 
                            yPos > tileSize - lineWidth;

                texture.SetPixel(x, y, isLine ? lineColor : tileColor);
            }
        }

        texture.Apply();

        // Criar e configurar o material
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.mainTexture = texture;
        
        // Configurar propriedades adicionais do material
        material.SetFloat("_Smoothness", 0.2f);
        material.SetFloat("_Metallic", 0.0f);
        
        // Aplicar o material
        GetComponent<MeshRenderer>().material = material;
    }

    // Método para atualizar o material em tempo de execução (se necessário)
    public void UpdateMaterial()
    {
        GenerateAndApplyMaterial();
    }
}