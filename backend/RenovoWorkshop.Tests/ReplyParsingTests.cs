using RenovoWorkshop.Infrastructure.Services;

namespace RenovoWorkshop.Tests;

public class ReplyParsingTests
{
    [Theory]
    [InlineData("sim", WhatsAppReplyProcessor.ReplyDecision.Approve)]
    [InlineData("Sim", WhatsAppReplyProcessor.ReplyDecision.Approve)]
    [InlineData("SIM!", WhatsAppReplyProcessor.ReplyDecision.Approve)]
    [InlineData("s", WhatsAppReplyProcessor.ReplyDecision.Approve)]
    [InlineData(" sim ", WhatsAppReplyProcessor.ReplyDecision.Approve)]
    [InlineData("sim OS-2026-003", WhatsAppReplyProcessor.ReplyDecision.Approve)]
    [InlineData("não", WhatsAppReplyProcessor.ReplyDecision.Reject)]
    [InlineData("nao", WhatsAppReplyProcessor.ReplyDecision.Reject)]
    [InlineData("n", WhatsAppReplyProcessor.ReplyDecision.Reject)]
    [InlineData("não.", WhatsAppReplyProcessor.ReplyDecision.Reject)]
    [InlineData("talvez", WhatsAppReplyProcessor.ReplyDecision.Unrecognized)]
    [InlineData("", WhatsAppReplyProcessor.ReplyDecision.Unrecognized)]
    public void ClassifyReply_ShouldInterpretCommonVariants(string rawText, WhatsAppReplyProcessor.ReplyDecision expected)
    {
        var normalized = WhatsAppReplyProcessor.NormalizeForMatch(rawText);
        var decision = WhatsAppReplyProcessor.ClassifyReply(normalized);

        Assert.Equal(expected, decision);
    }

    [Fact]
    public void NormalizeForMatch_ShouldStripAccentsPunctuationAndCase()
    {
        var result = WhatsAppReplyProcessor.NormalizeForMatch("  NÃO! ");

        Assert.Equal("nao", result);
    }
}
