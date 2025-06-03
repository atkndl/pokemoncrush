using UnityEngine;

public class PokeTopi : MonoBehaviour
{
    private string pokemonType;

    public void SetPokemonType(string type)
    {
        pokemonType = type;
    }

    public string GetPokemonType()
    {
        return pokemonType;
    }
}