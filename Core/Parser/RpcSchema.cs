namespace Core.Parser
{
    static class RpcSchema
    {
        public const string RpcRequestHeader = @"
/* Static RPC request header used for all request datagrams. */
readonly struct RpcRequestHeader {
    /*
        Identification for the caller to identify responses to this request.
	        
        The caller should ensure ids are always unique at the time of calling. Two active
        calls with the same id is undefined behavior. Re-using an id that is not currently
        in-flight is acceptable.

        These are unique per connection.
    */
    uint16 id;

    /*
        Function signature includes information about the args to ensure the caller and
        callee are referencing precisely the same thing. There is a non-zero risk of
        accidental signature collisions, but 32-bits is probably sufficient for peace of
        mind.

        I did some math, about a 26% chance of collision using 16-bits assuming 200 unique
        RPC calls which is pretty high, or <0.0005% chance with 32-bits.
    */
    uint32 signature;
}
";

        public const string RpcResponseHeader = @"
/* Static RPC response header used for all response datagrams. */
readonly struct RpcResponseHeader {
    /* The caller-assigned identifier */
	uint16 id;
}
";

        public const string RpcDatagram = @"
/*
    All data sent over the transport MUST be represented by this union.

    Note that data is sent as binary arrays to avoid depending on the generated structure
    definitions that we cannot know in this context. Ultimately the service will be
    responsible for determining how to interpret the data.
*/
union RpcDatagram {
    1 -> struct RpcRequestDatagram {
        RpcRequestHeader header;
        /* The function that is to be called. */
        uint16 opcode;
        /* Callee can decode this given the opcode in the header. */
        byte[] request;
    }

    2 -> struct RpcResponseOk {
        RpcResponseHeader header;
        /* Caller can decode this given the id in the header. */
        byte[] data;
    }

    3 -> struct RpcResponseErr {
        RpcResponseHeader header;
        /*
            User is responsible for defining what code values mean. These codes denote
            errors that happen only once user code is being executed and are specific
            to each domain.
        */
        uint32 code;
        /* An empty string is acceptable */
        string info;
    }

    /* Default response if no handler was registered. */
    0xfc -> struct CallNotSupported {
        RpcResponseHeader header;
    }

    /* Function id was unknown. */
    0xfd -> struct RpcResponseUnknownCall {
        RpcResponseHeader header;
    }

    /* The remote function signature did not agree with the expected signature. */
    0xfe -> struct RpcResponseInvalidSignature {
        RpcResponseHeader header;
        /* The remote function signature */
        uint32 signature;
    }

    /*
        A message received by the other end was unintelligible. This indicates a
        fundamental flaw with our encoding and possible bebop version mismatch.

        This should never occur between proper implementations of the same version.
    */
    0xff -> struct DecodeError {
        /* Information provided on a best-effort basis. */
        RpcResponseHeader header;
        string info;
    }
}
";
    }
}