import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { ITraderBase } from "@spt/models/eft/common/tables/ITrader";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { VFS } from "@spt/utils/VFS";
import path from "path";

import { NAMES } from "./names";


class Mod implements IPostDBLoadMod {
    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const db = container.resolve<DatabaseServer>("DatabaseServer").getTables();
        const traderTable = db.traders;

        const traders: Record<string, ITraderBase> = {};
        for (const trader of Object.values(traderTable)) {
            traders[trader.base.nickname] = trader.base;
        }

        for (const [oldName, newName] of Object.entries(NAMES)) {
            const trader = traders[oldName];

            if (!trader) {
                logger.info(`[TraderNamer] ${oldName} does not exist, not changing name.`);
                continue;
            }

            logger.info(`[TraderNamer] Changing ${oldName}'s name to ${newName}`);

            for (const locale of Object.values(db.locales.global)) {
                locale[`${trader._id} Nickname`] = newName;
            }
        }

        const vfs = container.resolve<VFS>("VFS");
        const imageRouter = container.resolve<ImageRouter>("ImageRouter");
        const imagesPath = path.resolve(__dirname, "../avatars");

        for (const image of vfs.getFiles(imagesPath)) {
            const [traderName, _] = image.split(".");
            const trader = traders[traderName];

            if (!trader) {
                logger.info(`[TraderNamer] ${traderName} does not exist, not changing avatar.`);
                continue;
            }

            logger.info(`[TraderNamer] Changing ${traderName}'s avatar.`);

            const avatarRouteNoExt = trader.avatar.split(".")[0];
            const imagePath = path.join(imagesPath, image);
            imageRouter.addRoute(avatarRouteNoExt, imagePath);
        }
    }
}

export const mod = new Mod();
