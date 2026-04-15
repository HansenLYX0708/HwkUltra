using System.Text.Json;
using HWKUltra.BarcodeScanner;
using HWKUltra.BarcodeScanner.Abstractions;
using HWKUltra.BarcodeScanner.Core;
using HWKUltra.BarcodeScanner.Implementations;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic barcode scanner builder for constructing controller and router from configuration.
    /// </summary>
    public class BarcodeScannerBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, IBarcodeScannerController> _controllerFactory;
        private Func<string, TConfig>? _jsonDeserializer;

        public BarcodeScannerBuilder(Func<TConfig, IBarcodeScannerController> controllerFactory)
        {
            _controllerFactory = controllerFactory;
        }

        public BarcodeScannerBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public BarcodeScannerBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer == null)
                throw new InvalidOperationException("JSON deserializer not configured. Call WithJsonDeserializer first.");
            _config = _jsonDeserializer(json);
            return this;
        }

        public IBarcodeScannerController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            return _controllerFactory(_config);
        }

        public BarcodeScannerRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            var controller = _controllerFactory(_config);
            return new BarcodeScannerRouter(controller);
        }
    }

    /// <summary>
    /// Dedicated barcode scanner builder for the default SerialBarcodeScannerController.
    /// </summary>
    public class BarcodeScannerBuilder
    {
        private SerialBarcodeScannerControllerConfig? _config;

        public BarcodeScannerBuilder FromJson(string json)
        {
            _config = JsonSerializer.Deserialize(json, BarcodeScannerJsonContext.Default.SerialBarcodeScannerControllerConfig);
            if (_config == null)
                throw new InvalidOperationException("Failed to deserialize SerialBarcodeScannerControllerConfig");
            return this;
        }

        public SerialBarcodeScannerController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            return new SerialBarcodeScannerController(_config);
        }

        public BarcodeScannerRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            var controller = new SerialBarcodeScannerController(_config);
            return new BarcodeScannerRouter(controller);
        }
    }
}
