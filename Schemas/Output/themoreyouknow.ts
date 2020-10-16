export namespace themoreyouknow {
  export enum VideoCodec {
      H264 = 0,
      H265 = 1
  }
  export interface IVideoData {
    timestamp: number
    width: number
    height: number
    fragment: Uint8Array
  }

  export const VideoData = {

    encode(message: IVideoData, view: PierogiView): Uint8Array | void {
      var source = !view;
      if (source) view = new PierogiView();

      if (message.timestamp != null) {
        view.writeFloat(message.timestamp);
      } else {
        throw new Error("Missing required field timestamp");
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

      if (source) return view.toArray();

    },

    decode(view: PierogiView | Uint8Array): IVideoData {
      if (!(view instanceof PierogiView)) {
        view = new PierogiView(view);
      }

      var message: IVideoData = {
          timestamp: view.readFloat(),
          width: view.readUint(),
          height: view.readUint(),
          fragment: view.readBytes(),
      };

      return message;
    }
  };

  export interface IMediaMessage {
    codec?: VideoCodec
    /**
     * @deprecated reason
     */
    friendlyName?: string
    data?: IVideoData
  }

  export const MediaMessage = {

    encode(message: IMediaMessage, view: PierogiView): Uint8Array | void {
      var source = !view;
      if (source) view = new PierogiView();

      if (message.codec != null) {
        view.writeUint(1);
        view.writeEnum(message.codec);
      }

      if (message.data != null) {
        view.writeUint(3);
        VideoData.encode(message.data, view);
      }
      view.writeUint(0);

      if (source) return view.toArray();

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
            view.readString();
            break;

          case 3:
            message.data = VideoData.decode(view);
            break;

          default:
            throw new Error("Attempted to parse invalid message");
        }
      }
    }
  };

}