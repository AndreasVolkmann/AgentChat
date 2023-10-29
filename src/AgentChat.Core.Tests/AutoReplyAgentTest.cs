﻿using AgentChat.Example.Share;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;

namespace AgentChat.Core.Tests
{
    public class AutoReplyAgentTest
    {
        [Fact]
        public async Task AutoReplyAsyncTest()
        {
            var echoAgent = new EchoAgent("echo");
            IAgent alice = echoAgent
                .CreateAutoReplyAgent("Alice", async (conversations, ct) => new Message(ChatRole.Assistant, "I'm your father", from: "Alice"));

            var msg = new Message(ChatRole.User, "hey", from: "Bob");
            var reply = await alice.SendMessageAsync(msg);
            reply.From.Should().Be("Alice");
            reply.Content.Should().Be("I'm your father");
            reply.Role.Should().Be(ChatRole.Assistant);
        }


        [Fact]
        public async Task PreprocessFunctionTestAsync()
        {
            IAgent echoAgent = new EchoAgent("echo");
            echoAgent = echoAgent.CreatePreprocessAgent("echo", async (msgs, ct) =>
            {
                return msgs.Select(msg => new Message(msg.Role, msg.Content?.ToUpper(), msg.From));
            });

            var msg = new Message(ChatRole.User, "heLLo", "Bob");
            var reply = await echoAgent.SendMessageAsync(msg);

            reply.Content.Should().Be("HELLO");
            reply.From.Should().Be("echo");
        }

        [Fact]
        public async Task PostProcessFunctionTestAsync()
        {
            IAgent echoAgent = new EchoAgent("echo");
            echoAgent = echoAgent.CreatePostProcessAgent("echo", async (chatHistory, reply, ct) =>
            {
                return new Message(reply.Role, reply.Content?.ToUpper(), reply.From);
            });

            var msg = new Message(ChatRole.User, "heLLo", "Bob");
            var reply = await echoAgent.SendMessageAsync(msg);

            reply.Content.Should().Be("HELLO");
            reply.From.Should().Be("echo");
        }
    }
}
