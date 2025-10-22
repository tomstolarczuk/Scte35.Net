namespace Scte35.Net.Model.Enums;

public enum EncryptionAlgorithm : byte
{
    None = 0,
    DesEcbMode = 1,
    DesCbcMode = 2,
    TripleDesEcbMode = 3,
    TripleDesCbcMode = 4,
    Aes128Ecm = 5,
    Aes128Cbc = 6
}
