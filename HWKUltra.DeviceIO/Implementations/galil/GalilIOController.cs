using HWKUltra.DeviceIO.Abstractions;

namespace HWKUltra.DeviceIO.Implementations.galil
{
    /// <summary>
    /// Galil IO controller implementation (refactored from IOControlLib).
    /// Supports multiple cards, communicates with Galil hardware via gclib.
    /// </summary>
    public class GalilIOController : IIOController
    {
        private readonly GalilIOConfig _config;
        private readonly Dictionary<int, gclib> _cards = new();
        private readonly object _commandLock = new();
        private bool _isOpen;

        public GalilIOController(GalilIOConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Open()
        {
            if (_isOpen) return;

            try
            {
                foreach (var cardConfig in _config.Cards)
                {
                    var g = new gclib();
                    g.GOpen(cardConfig.IpAddress);
                    _cards[cardConfig.CardIndex] = g;
                }
                _isOpen = true;
            }
            catch
            {
                // Clean up already-opened connections
                foreach (var card in _cards.Values)
                {
                    try { card.GClose(); } catch { }
                }
                _cards.Clear();
                throw;
            }
        }

        public void Close()
        {
            if (!_isOpen) return;

            foreach (var card in _cards.Values)
            {
                try { card.GClose(); } catch { }
            }
            _cards.Clear();
            _isOpen = false;
        }

        public void SetOutput(int cardIndex, int bitIndex, bool value)
        {
            var card = GetCard(cardIndex);
            string command = (value ? "SB" : "CB") + bitIndex.ToString();

            lock (_commandLock)
            {
                card.GCommand(command);
            }
        }

        public bool GetOutput(int cardIndex, int bitIndex)
        {
            int bankIndex = bitIndex / 8;
            int bitInBank = bitIndex % 8;

            int bankValue = ReadOutputBank(cardIndex, bankIndex);
            return ((bankValue >> bitInBank) & 1) != 0;
        }

        public bool GetInput(int cardIndex, int bitIndex)
        {
            int bankIndex = bitIndex / 8;
            int bitInBank = bitIndex % 8;

            int bankValue = ReadInputBank(cardIndex, bankIndex);
            return ((bankValue >> bitInBank) & 1) != 0;
        }

        public int ReadInputBank(int cardIndex, int bankIndex)
        {
            var card = GetCard(cardIndex);
            lock (_commandLock)
            {
                string response = card.GCommand($"MG_TI{bankIndex}");
                return (int)Convert.ToDouble(response);
            }
        }

        public int ReadOutputBank(int cardIndex, int bankIndex)
        {
            var card = GetCard(cardIndex);
            lock (_commandLock)
            {
                string response = card.GCommand($"MG_OP{bankIndex}");
                return (int)Convert.ToDouble(response);
            }
        }

        private gclib GetCard(int cardIndex)
        {
            if (!_isOpen)
                throw new InvalidOperationException("IO controller is not open");

            if (!_cards.TryGetValue(cardIndex, out var card))
                throw new ArgumentException($"Card index {cardIndex} not found. Available: {string.Join(", ", _cards.Keys)}");

            return card;
        }
    }
}
