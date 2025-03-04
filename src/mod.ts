import { DependencyContainer } from "tsyringe";

import { ITraderBase } from "@spt/models/eft/common/tables/ITrader";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { DatabaseService } from "@spt/services/DatabaseService";
import { FileSystemSync } from "@spt/utils/FileSystemSync";
import path from "path";

import { NAMES } from "./names";


class Mod implements IPostDBLoadMod {
    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const db = container.resolve<DatabaseService>("DatabaseService");
        const locales = db.getLocales();
        const en = locales.global.en;

        const traders: Record<string, ITraderBase> = Object.fromEntries(
            Object.values(db.getTraders())
                .map((trader) => [en[`${trader.base._id} Nickname`], trader.base])
                .filter((([nick, _]) => (nick !== undefined))));

        for (const [oldName, newName] of Object.entries(NAMES)) {
            const trader = traders[oldName];
            var details = newName;
            if ((typeof newName) === "string") {
                details = { Nickname: newName as string }
            }

            if (!trader) {
                logger.info(`[TraderNamer] ${oldName} does not exist, not changing name.`);
                continue;
            }

            for (const [key, value] of Object.entries(details)) {
                logger.info(`[TraderNamer] Changing ${oldName}'s ${key} to ${value}`);
            }

            for (const locale of Object.values(locales.global)) {
                for (const [detail, value] of Object.entries(details)) {
                    locale[`${trader._id} ${detail}`] = value;
                }
            }
        }

        const fileSystem = container.resolve<FileSystemSync>("FileSystemSync");
        const imageRouter = container.resolve<ImageRouter>("ImageRouter");
        const imagesPath = path.resolve(__dirname, "../avatars");

        for (const image of fileSystem.getFiles(imagesPath)) {
            // getFiles gives names with leading ‘/’
            const traderName = image.split(".")[0].substring(1);
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
