export namespace the_more_you_know {
  export enum VideoCodec {
      H264 = 0,
      H265 = 1
  }
  export interface IVideoData {
    readonly timestamps: number
    readonly width: number
    readonly height: number
    readonly fragment: Uint8Array
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

      if (isTopLevel) return view.toArray();

    },

    decode(view: PierogiView | Uint8Array): IVideoData {
      if (!(view instanceof PierogiView)) {
        view = new PierogiView(view);
      }

      var message: IVideoData = {
          timestamps: view.readFloat(),
          width: view.readUint(),
          height: view.readUint(),
          fragment: view.readBytes(),
      };

      return message;
    }
  };

  export interface IMyMessage {
    cats?: number
    dogs?: number
  }

  export const MyMessage = {

    encode(message: IMyMessage, view: PierogiView): Uint8Array | void {
      var isTopLevel = !view;
      if (isTopLevel) view = new PierogiView();

      if (message.cats != null) {
        view.writeUint(1);
        view.writeUint(message.cats);
      }

      if (message.dogs != null) {
        view.writeUint(2);
        view.writeUint(message.dogs);
      }
      view.writeUint(0);

      if (isTopLevel) return view.toArray();

    },

    decode(view: PierogiView | Uint8Array): IMyMessage {
      if (!(view instanceof PierogiView)) {
        view = new PierogiView(view);
      }

      let message: IMyMessage = {};
      while (true) {
        switch (view.readUint()) {
          case 0:
            return message;

          case 1:
            message.cats = view.readUint();
            break;

          case 2:
            message.dogs = view.readUint();
            break;

          default:
            throw new Error("Attempted to parse invalid message");
        }
      }
    }
  };

  export interface IMediaMessage {
    codecs?: VideoCodec[]
    otherCodec?: VideoCodec
    data?: IVideoData
    /**
     * @deprecated why
     */
    mine?: IMyMessage
  }

  export const MediaMessage = {

    encode(message: IMediaMessage, view: PierogiView): Uint8Array | void {
      var isTopLevel = !view;
      if (isTopLevel) view = new PierogiView();

      if (message.codecs != null) {
        view.writeUint(1);
        view.writeUint(message.codecs.length);
        for (var i = 0; i < message.codecs.length; i++) {
          view.writeEnum(message.codecs[i]);
        }
      }

      if (message.otherCodec != null) {
        view.writeUint(2);
        view.writeEnum(message.otherCodec);
      }

      if (message.data != null) {
        view.writeUint(3);
        VideoData.encode(message.data, view);
      }
      view.writeUint(0);

      if (isTopLevel) return view.toArray();

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
            let length = view.readUint();
            message.codecs = new Array<VideoCodec>(length);
            while (length-- > 0) message.codecs.push(view.readUint() as VideoCodec);
            break;

          case 2:
            message.otherCodec = view.readUint() as VideoCodec;
            break;

          case 3:
            message.data = VideoData.decode(view);
            break;

          case 4:
            MyMessage.decode(view);
            break;

          default:
            throw new Error("Attempted to parse invalid message");
        }
      }
    }
  };

}