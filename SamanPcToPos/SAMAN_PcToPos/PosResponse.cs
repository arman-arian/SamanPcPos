namespace SamanPcToPos.SAMAN_PcToPos
{
    public class PosResponse
    {
        public PosResponse()
        {
        }

        public PosResponse(string terminalId, string serialNumber, string batchNumber, string customerPan, string dateTime, string responseCode, string referenceNumber, string traceNumber, string message, string amount, string affectedAmount, bool confirmTransaction)
        {
            this.TerminalId = terminalId;
            this.SerialNumber = serialNumber;
            this.BatchNumber = batchNumber;
            this.CustomerPan = customerPan;
            this.DateTime = dateTime;
            this.ResponseCode = responseCode;
            this.ReferenceNumber = referenceNumber;
            this.TraceNumber = traceNumber;
            this.Message = message;
            this.ConfirmTransaction = confirmTransaction;
            this.Amount = amount;
            this.AffectedAmount = affectedAmount;
        }

        public string AffectedAmount { get; set; }

        public string Amount { get; set; }

        public string BatchNumber { get; set; }

        public bool ConfirmTransaction { get; set; }

        public string CustomerPan { get; set; }

        public string DateTime { get; set; }

        public string Message { get; set; }

        public string ReferenceNumber { get; set; }

        public string ResponseCode { get; set; }

        public string SerialNumber { get; set; }

        public string TerminalId { get; set; }

        public string TraceNumber { get; set; }
    }
}

