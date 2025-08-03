using Asteroides;
using Microsoft.Xna.Framework.Input;
using Monogame.Processing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Cliente.Servicos;

namespace Asteroides.Cliente;

public class JogoAsteroides : Processing
{
    private GerenciadorDeRede _gerenciadorDeRede;

    readonly List<Nave> naves = new();
    readonly List<Tiro> tiros = new();
    readonly List<Asteroide> asteroides = new();

    PImage spriteNave, spriteAsteroide, spriteTiro;

    int pontuacao;

    bool esquerda, direita, cima, baixo;

    public JogoAsteroides(GerenciadorDeRede gerenciadorDeRede)
    {
        this._gerenciadorDeRede = gerenciadorDeRede;
    }

    public override void Setup()
    {
        size(1280, 720);

        spriteNave = loadImage("Content/nave.png");
        spriteAsteroide = loadImage("Content/AsteroidBrown.png");
        spriteTiro = loadImage("Content/tiro.png");

        nave = new Nave(new Vector2(width / 2f, height - 60), spriteNave, spriteTiro);
    }

    public override async void Draw()
    {

    }

    /* ====================== input ============================= */
    public void Teclas()
    {
        esquerda = false;
        direita = false;
        cima = false;
        baixo = false;

        if (!keyPressed) return;  // nada pressionado

        /* tecla “única” (letras) */
        switch (char.ToUpperInvariant(key))
        {
            case 'A': esquerda = true; break;
            case 'D': direita = true; break;
            case 'W': cima = true; break;
            case 'S': baixo = true; break;
        }

        /* teclas especiais (setas, espaço, esc) */
        switch (keyCode)
        {
            case Keys.Left: esquerda = true; break;
            case Keys.Right: direita = true; break;
            case Keys.Up: cima = true; break;
            case Keys.Down: baixo = true; break;

            case Keys.Space: tiros.Add(nave.Atirar()); break;
            case Keys.Escape: Exit(); break;
        }
    }

    public override void KeyReleased(Keys pkey)
    {
        switch (char.ToUpperInvariant(key))
        {
            case 'A': esquerda = false; break;
            case 'D': direita = false; break;
            case 'W': cima = false; break;
            case 'S': baixo = false; break;
        }

        switch (keyCode)
        {
            case Keys.Left: esquerda = false; break;
            case Keys.Right: direita = false; break;
            case Keys.Up: cima = false; break;
            case Keys.Down: baixo = false; break;
        }
    }

    /* ====================== fábrica de asteroides ============= */
    Asteroide NovoAsteroide()
    {
        float x = rnd.Next(width);
        float velY = 2f + (float)rnd.NextDouble() * 2f;   // 2–4 px/frame
        return new Asteroide(new Vector2(x, -30), new Vector2(0, velY), 25, spriteAsteroide);
    }
}
