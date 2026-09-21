# Manual: implementar "Trocar Senha" na MainWindow

Este projeto é derivado do meu TCC (magal/TCCAJUSTADO). Lá já existe a funcionalidade de troca de senha via um dialog "Meu Perfil", acessado por um botão de menu na MainWindow. Preciso que você replique essa funcionalidade neste projeto, adaptando aos nomes de classes/tabelas que já existem aqui.

## 1. Onde o botão fica (MainWindow)

No projeto original, existe um botão de perfil no rodapé do menu lateral que abre um `ContextMenu` com opções, entre elas "Meu Perfil":

```xml
<Button Style="{StaticResource MenuButtonStyle}"
        Padding="20,10"
        Click="BtnPerfil_Click">
    <Button.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Meu Perfil" Click="MenuMeuPerfil_Click">
                <MenuItem.Icon>
                    <TextBlock Text="🙍" VerticalAlignment="Center"/>
                </MenuItem.Icon>
            </MenuItem>
            <!-- outros itens do menu -->
        </ContextMenu>
    </Button.ContextMenu>
</Button>
```

No code-behind da MainWindow:

```csharp
private void BtnPerfil_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button btn && btn.ContextMenu != null)
    {
        btn.ContextMenu.PlacementTarget = btn;
        btn.ContextMenu.IsOpen = true;
    }
}

private void MenuMeuPerfil_Click(object sender, RoutedEventArgs e)
{
    var dialog = new MeuPerfilDialog();
    dialog.Owner = this;
    dialog.ShowDialog();
}
```

Se este projeto já tem um botão/menu de perfil ou de usuário logado, o mais simples é adicionar um item de menu "Trocar Senha" (ou "Meu Perfil") ali e abrir um novo dialog no `Click`. Se não existir nada parecido, pode ser um botão simples em algum canto da MainWindow (ex: header) que abre o mesmo dialog.

## 2. O Dialog de troca de senha (`MeuPerfilDialog`)

Um `Window` (pode ser `UserControl`/dialog conforme o padrão daqui) com 3 campos de senha:

- Senha atual
- Nova senha
- Confirmar nova senha

XAML (resumido, adaptar estilos ao tema do projeto):

```xml
<TextBlock Text="SENHA ATUAL"/>
<PasswordBox x:Name="TxtSenhaAtual"/>

<TextBlock Text="NOVA SENHA"/>
<PasswordBox x:Name="TxtSenhaNova"/>

<TextBlock Text="CONFIRMAR NOVA SENHA"/>
<PasswordBox x:Name="TxtConfirmarSenhaNova"/>

<Button Content="ALTERAR SENHA" Click="BtnSalvar_Click"/>
```

Code-behind com a lógica de validação e troca:

```csharp
private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
{
    if (UsuarioLogado == null) // substituir pela referência ao usuário logado deste projeto
    {
        MessageBox.Show("Sessão inválida. Faça login novamente.");
        return;
    }

    if (string.IsNullOrWhiteSpace(TxtSenhaAtual.Password) ||
        string.IsNullOrWhiteSpace(TxtSenhaNova.Password) ||
        string.IsNullOrWhiteSpace(TxtConfirmarSenhaNova.Password))
    {
        MessageBox.Show("Preencha todos os campos.");
        return;
    }

    if (!PasswordHasher.Verify(TxtSenhaAtual.Password, UsuarioLogado.senha))
    {
        MessageBox.Show("A senha atual informada está incorreta.");
        return;
    }

    if (TxtSenhaNova.Password != TxtConfirmarSenhaNova.Password)
    {
        MessageBox.Show("A nova senha e a confirmação não coincidem.");
        return;
    }

    try
    {
        string novoHash = PasswordHasher.Hash(TxtSenhaNova.Password);
        await _repository.AtualizarSenha(UsuarioLogado.id, novoHash);
        UsuarioLogado.senha = novoHash;

        MessageBox.Show("Senha alterada com sucesso!");
        DialogResult = true;
        Close();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Erro ao alterar senha: {ex.Message}");
    }
}
```

Pontos importantes:
- Sempre exigir a senha atual antes de trocar (evita que alguém com sessão aberta troque a senha de outro sem confirmar).
- Validar que "nova senha" e "confirmar nova senha" batem.
- Nunca salvar a senha em texto puro — usar hash.

## 3. Hash de senha (`PasswordHasher`)

Se este projeto ainda não tem um hasher de senha, usar PBKDF2 (built-in do .NET, não precisa de lib externa):

```csharp
using System;
using System.Security.Cryptography;

public static class PasswordHasher
{
    private const string Prefixo = "V1";
    private const int Iteracoes = 100_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    public static string Hash(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string senhaDigitada, string valorArmazenado)
    {
        if (senhaDigitada == null || valorArmazenado == null) return false;

        if (!valorArmazenado.StartsWith(Prefixo + "$", StringComparison.Ordinal))
            return senhaDigitada == valorArmazenado; // compatibilidade com senhas legadas em texto puro, se houver

        string[] partes = valorArmazenado.Split('$');
        if (partes.Length != 4) return false;
        if (!int.TryParse(partes[1], out int iteracoes)) return false;

        byte[] salt = Convert.FromBase64String(partes[2]);
        byte[] hashEsperado = Convert.FromBase64String(partes[3]);
        byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senhaDigitada, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
```

**Se o projeto já tem um jeito de fazer hash de senha (ex: BCrypt, outra lib), usar o que já existe em vez de introduzir este.**

## 4. Atualizar a senha no banco

Método no repositório de usuário (adaptar nome de tabela/coluna/ORM ao projeto):

```csharp
public async Task AtualizarSenha(int idUsuario, string novoHashSenha)
{
    using (var conn = /* obter conexão como já é feito no repositório */)
    {
        await conn.OpenAsync();
        string sql = "UPDATE usuario SET senha = @senha WHERE id_usuario = @id";
        using (var cmd = new MySqlCommand(sql, conn)) // trocar pelo provider usado no projeto
        {
            cmd.Parameters.AddWithValue("@senha", novoHashSenha);
            cmd.Parameters.AddWithValue("@id", idUsuario);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
```

## 5. Resumo do que fazer

1. Achar (ou criar) um menu/botão de "usuário logado" na MainWindow.
2. Adicionar item "Trocar Senha" / "Meu Perfil" que abre um novo dialog.
3. Criar o dialog com 3 `PasswordBox` (atual, nova, confirmar) e botão "Alterar Senha".
4. No clique, validar campos, verificar a senha atual com o hasher existente (ou criar um PBKDF2 como acima), gerar hash da nova senha e persistir via repositório.
5. Atualizar o usuário da sessão em memória com o novo hash, para não exigir logout/login.
