namespace CQLE_MIGRACAO.Models
{
  public class LoginResult
  {
    public bool Sucesso { get; }
    public string Mensagem { get; }

    public LoginResult(bool sucesso, string mensagem = "")
    {
      Sucesso = sucesso;
      Mensagem = mensagem;
    }
  }
}
