export namespace the_more_you_know {
    export enum VideoCodec {
        H264 = 0,
        H265 = 1
    }
    export interface IVideoData {
        timestamps: number;
        width: number;
        height: number;
        fragment: Uint8Array;
    }

    export const VideoData = {
        encode(message: IVideoData, view: PierogiView): Uint8Array | void {
            var isTopLevel = !view;
            if (isTopLevel) view = new PierogiView();

            if (message.timestamps != null) {
                view.writeFloat(message.timestamps);
            } else {
                throw new Error("Missing required field timestamps");
            }

            if (message.width != null) {
                view.writeUint(message.width);
            } else {
                throw new Error("Missing required field width");
            }

            if (message.height != null) {
                view.writeUint(message.height);
            } else {
                throw new Error("Missing required field height");
            }

            if (message.fragment != null) {
                view.writeBytes(message.fragment);
            } else {
                throw new Error("Missing required field fragment");
            }

            if (isTopLevel) return view.toUint8Array();
        },

        decode(view: PierogiView | Uint8Array): IVideoData {
            if (!(view instanceof PierogiView)) {
                view = new PierogiView(view);
            }

            var message: IVideoData;
            message = ((() => { }) as unknown) as IVideoData;

            message.timestamps = view.readFloat();
            message.width = view.readUint();
            message.height = view.readUint();
            message.fragment = view.readBytes();
            return message;
        }
    };

    export interface IMediaMessage {
        codec?: VideoCodec;
        data?: IVideoData;
    }

    export const MediaMessage = {
        encode(message: IMediaMessage, view: PierogiView): Uint8Array | void {
            var isTopLevel = !view;
            if (isTopLevel) view = new PierogiView();

            if (message.codec != null) {
                view.writeUint(1);
                var encoded = (VideoCodec[message.codec] as unknown) as number;
                if (encoded === void 0) throw new Error("");
                view.writeUint(encoded);
            }

            if (message.data != null) {
                view.writeUint(2);
                VideoData.encode(message.data, view);
            }
            view.writeUint(0);

            if (isTopLevel) return view.toUint8Array();
        },

        decode(view: PierogiView | Uint8Array): IMediaMessage {
            if (!(view instanceof PierogiView)) {
                view = new PierogiView(view);
            }

            let message: IMediaMessage = {};
            while (true) {
                switch (view.readUint()) {
                    case 0:
                        return message;

                    case 1:
                        message.codec = view.readUint() as VideoCodec;
                        break;

                    case 2:
                        message.data = VideoData.decode(view);
                        break;

                    default:
                        throw new Error("Attempted to parse invalid message");
                }
            }
        }
    };
}