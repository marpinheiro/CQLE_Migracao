using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace CQLE_MIGRACAO.Services
{
  public static class TrialSystem
  {
    // Caminho no Registro do Windows
    private const string REGISTRY_PATH = @"SOFTWARE\CQLE_MIGRACAO_TRIAL";
    private const string REGISTRY_KEY = "LicenseData";

    // Chave interna
    private const string INTERNAL_KEY = "CQLE_MIGRACAO_2026_KEY_SECURE";

    public enum TrialStatus
    {
      Valid,
      Expired,
      Corrupted,
      ClockTampered
    }

    public static (TrialStatus status, int daysLeft) CheckTrial(int trialPeriodDays)
    {
      try
      {
        // Correção de Nulidade: Usamos 'RegistryKey?' porque pode vir nulo
        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, true))
        {
          // 1. PRIMEIRA EXECUÇÃO (Se a chave for nula ou o valor não existir)
          if (key == null || key.GetValue(REGISTRY_KEY) == null)
          {
            InitializeTrial();
            return (TrialStatus.Valid, trialPeriodDays);
          }

          // 2. LEITURA DOS DADOS (Tratamento seguro de nulo)
          object? value = key.GetValue(REGISTRY_KEY);
          if (value == null) return (TrialStatus.Corrupted, 0);

          string encryptedData = value.ToString() ?? string.Empty;
          string decryptedData = Decrypt(encryptedData);

          var parts = decryptedData.Split('|');
          if (parts.Length != 2) return (TrialStatus.Corrupted, 0);

          DateTime startDate = DateTime.Parse(parts[0]);
          DateTime lastRunDate = DateTime.Parse(parts[1]);
          DateTime now = DateTime.Now;

          // 3. VALIDAÇÃO DE RELÓGIO
          if (now < lastRunDate.AddHours(-1))
          {
            return (TrialStatus.ClockTampered, 0);
          }

          // 4. ATUALIZA A ÚLTIMA EXECUÇÃO
          UpdateLastRun(startDate, now);

          // 5. CÁLCULO DE DIAS
          TimeSpan usedTime = now - startDate;
          int daysRemaining = trialPeriodDays - (int)usedTime.TotalDays;

          if (daysRemaining < 0)
            return (TrialStatus.Expired, 0);

          return (TrialStatus.Valid, daysRemaining);
        }
      }
      catch
      {
        return (TrialStatus.Corrupted, 0);
      }
    }

    private static void InitializeTrial()
    {
      using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH))
      {
        // CreateSubKey dificilmente retorna null, mas o compilador avisa
        if (key == null) return;

        DateTime now = DateTime.Now;
        string data = $"{now}|{now}";
        string encrypted = Encrypt(data);
        key.SetValue(REGISTRY_KEY, encrypted);
      }
    }

    private static void UpdateLastRun(DateTime startDate, DateTime now)
    {
      using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, true))
      {
        if (key != null)
        {
          string data = $"{startDate}|{now}";
          string encrypted = Encrypt(data);
          key.SetValue(REGISTRY_KEY, encrypted);
        }
      }
    }

    // === CRIPTOGRAFIA AJUSTADA PARA .NET 8 (Correção SYSLIB0041) ===
    private static string Encrypt(string clearText)
    {
      byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
      using (Aes encryptor = Aes.Create())
      {
        // Agora especificamos HashAlgorithmName.SHA256 explicitamente
        using (var pdb = new Rfc2898DeriveBytes(INTERNAL_KEY, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA256))
        {
          encryptor.Key = pdb.GetBytes(32);
          encryptor.IV = pdb.GetBytes(16);
        }

        using (MemoryStream ms = new MemoryStream())
        {
          using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
          {
            cs.Write(clearBytes, 0, clearBytes.Length);
          }
          return Convert.ToBase64String(ms.ToArray());
        }
      }
    }

    private static string Decrypt(string cipherText)
    {
      if (string.IsNullOrEmpty(cipherText)) return string.Empty;

      byte[] cipherBytes = Convert.FromBase64String(cipherText);
      using (Aes encryptor = Aes.Create())
      {
        // Correção aqui também: SHA256 explícito
        using (var pdb = new Rfc2898DeriveBytes(INTERNAL_KEY, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA256))
        {
          encryptor.Key = pdb.GetBytes(32);
          encryptor.IV = pdb.GetBytes(16);
        }

        using (MemoryStream ms = new MemoryStream())
        {
          using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
          {
            cs.Write(cipherBytes, 0, cipherBytes.Length);
          }
          return Encoding.Unicode.GetString(ms.ToArray());
        }
      }
    }
  }
}