namespace MasterFramework.Tutorial
{
    public class PiecePlacedEvent
    {
        public string PieceId { get; }
        public string PlacedTargetId { get; }

        public PiecePlacedEvent(string pieceId, string placedTargetId)
        {
            PieceId = pieceId;
            PlacedTargetId = placedTargetId;
        }
    }
}
